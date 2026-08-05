using System.Text;
using Nexora.Api.Cloud.Hubs;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Api.Cloud.Infrastructure.Auth;
using Nexora.Api.Cloud.Infrastructure.Idempotency;
using Nexora.Api.Cloud.Infrastructure.Observability;
using Nexora.Api.Cloud.Realtime;
using Nexora.Api.Cloud.Workers;
using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Idempotency;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Abstractions.Storage;
using Nexora.Application.Auth.Shared;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Devices.Abstractions;
using Nexora.Application.Installation.Abstractions;
using Nexora.Application.Installations.Abstractions;
using Nexora.Application.Operation.Abstractions;
using Nexora.Contracts.Http;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Catalog;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Idempotency;
using Nexora.Infrastructure.Installation;
using Nexora.Infrastructure.Installations;
using Nexora.Infrastructure.Notifications;
using Nexora.Infrastructure.Operation;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Interceptors;
using Nexora.Infrastructure.Platform;
using Nexora.Infrastructure.Realtime;
using Nexora.Infrastructure.Storage;
using Nexora.Shared.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Sentry (só Api.Cloud referencia o pacote) — inofensivo sem DSN configurado:
// o SDK simplesmente não envia eventos (não trava o boot em dev/CI).
// ---------------------------------------------------------------------------
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options => options.Dsn = sentryDsn);
}

// ---------------------------------------------------------------------------
// Persistência (ADR-038): AppDbContext + TenantConnectionInterceptor (RLS via
// SET LOCAL app.tenant_id). Scoped de propósito — ver nota equivalente em
// Nexora.Api.Edge/Program.cs.
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention();
    options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>(), sp.GetRequiredService<AuditTraceInterceptor>());
});
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<TenantConnectionInterceptor>();
// AuditTraceInterceptor (E-09/US-090) só lê Activity.Current — sem estado por requisição, por
// isso Singleton (ao contrário do TenantConnectionInterceptor acima, que precisa variar por escopo).
builder.Services.AddSingleton<AuditTraceInterceptor>();

// ---------------------------------------------------------------------------
// CQRS/MediatR (ADR-037) — mesmo pipeline do Api.Edge.
// ---------------------------------------------------------------------------
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
// ADR-021: preenche code/recoverable/requiresAuthorization/traceId nos ProblemDetails que o
// PRÓPRIO framework monta (401/403/404/500 sem passar por um Result nosso) — ver docstring de
// ResultExtensions.EnrichFrameworkProblemDetails.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ResultExtensions.EnrichFrameworkProblemDetails);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Idempotência de escrita (ADR-020) — porta em Application, implementação sobre AppDbContext em
// Infrastructure; o middleware que efetivamente intercepta a requisição só pode viver aqui (Api),
// precisa de HttpContext/RequestDelegate (ASP.NET Core, proibido em Infrastructure — ADR-039).
builder.Services.AddScoped<IIdempotencyStore, IdempotencyStore>();

// US-030 §8: geração de short_code (ADR-016) com lock consultivo do Postgres — só Infrastructure
// fala Npgsql/SQL cru (ADR-039).
builder.Services.AddScoped<IOrderShortCodeAllocator, OrderShortCodeAllocator>();

// ---------------------------------------------------------------------------
// Options (IOptions<T>).
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AuthSecretsOptions>(builder.Configuration.GetSection(AuthSecretsOptions.SectionName));
builder.Services.Configure<EmailOutboxOptions>(builder.Configuration.GetSection(EmailOutboxOptions.SectionName));
builder.Services.Configure<S3BrandingStorageOptions>(builder.Configuration.GetSection(S3BrandingStorageOptions.SectionName));
builder.Services.Configure<FileSystemBackupStorageOptions>(builder.Configuration.GetSection(FileSystemBackupStorageOptions.SectionName));
builder.Services.Configure<CloudPublicUrlSettings>(builder.Configuration.GetSection(CloudPublicUrlSettings.SectionName));
builder.Services.Configure<PinLookupMasterKeyOptions>(builder.Configuration.GetSection(PinLookupMasterKeyOptions.SectionName));
builder.Services.Configure<AppVersionOptions>(builder.Configuration.GetSection(AppVersionOptions.SectionName));

// ---------------------------------------------------------------------------
// Segurança / Auth — consumidores expostos pelo Api.Cloud (login por senha +
// MFA, refresh de sessão, convite de dono, provisionamento de tenant).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<ICredentialHasher, Argon2CredentialHasher>();
builder.Services.AddSingleton<IPinLookupDigester, HmacPinLookupDigester>();
builder.Services.AddSingleton<IOtpVerifier, TotpOtpVerifier>();
builder.Services.AddSingleton<IMfaSecretCipher, AesGcmMfaSecretCipher>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<ICurrentTenantContext, CloudCurrentTenantContext>();

// SignalR (US-015) — propagação em tempo real de product.unavailable/product.available, réplica
// idêntica de Nexora.Api.Edge/Program.cs (ver CatalogAvailabilityHub).
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAvailabilityBroadcaster, SignalRAvailabilityBroadcaster>();

// IAlertsBroadcaster/ITableMapBroadcaster/IOrderConsumptionBroadcaster (US-023/024/025/026) — mesa,
// comanda e alerta em tempo real são autoridade do edge (CLAUDE.md, "pedido é criado no local");
// Api.Cloud não tem controller que dispare esses handlers, mas MediatR registra todo handler do
// assembly Application, então a porta ainda precisa resolver — no-op aqui (ver Null*Broadcaster).
builder.Services.AddSingleton<IAlertsBroadcaster, NullAlertsBroadcaster>();
builder.Services.AddSingleton<ITableMapBroadcaster, NullTableMapBroadcaster>();
builder.Services.AddSingleton<IOrderConsumptionBroadcaster, NullOrderConsumptionBroadcaster>();
// IStationBroadcaster (US-031) — roteamento por praça também é autoridade exclusiva do edge (KDS
// vive na LAN da loja); mesmo raciocínio no-op acima.
builder.Services.AddSingleton<IStationBroadcaster, NullStationBroadcaster>();
// ISyncStatusBroadcaster (US-034) — detectar/anunciar a própria queda de internet é conceito
// exclusivo do edge (é ele que fala com a nuvem, nunca o inverso); mesmo raciocínio no-op acima.
builder.Services.AddSingleton<ISyncStatusBroadcaster, NullSyncStatusBroadcaster>();

// Devices — geração de código de pareamento e de segredo do dispositivo (réplica de
// Nexora.Api.Edge/Program.cs). Pareamento acontece com a loja (edge), não com a nuvem, mas o
// handler ainda precisa resolver a porta pelo mesmo motivo acima.
builder.Services.AddSingleton<IPairingCodeGenerator, PairingCodeGenerator>();
builder.Services.AddSingleton<IDeviceSecretGenerator, DeviceSecretGenerator>();

// Installation bootstrap/health (réplica de Nexora.Api.Edge/Program.cs) — ImportBootstrapCommand e
// GetInstallationHealthQuery/PollSyncHealthCommand não são chamados por nenhum controller do
// Api.Cloud (a nuvem é o alvo checado, não quem faz bootstrap/poll), mas precisam resolver:
// IBootstrapCatalogImporter/IBootstrapAuthorizationImporter e IRedisHealthChecker reaproveitam a
// mesma implementação do edge (degradam sem lançar exceção); ISyncHealthPoller usa um no-op porque
// a implementação real depende de infraestrutura exclusiva do edge (IInstallationRequestSigner).
builder.Services.AddSingleton<IBootstrapCatalogImporter, NullBootstrapCatalogImporter>();
builder.Services.AddSingleton<IBootstrapAuthorizationImporter, NullBootstrapAuthorizationImporter>();
builder.Services.Configure<RedisHealthCheckOptions>(builder.Configuration.GetSection(RedisHealthCheckOptions.SectionName));
builder.Services.AddSingleton<IRedisHealthChecker, RedisHealthChecker>();
builder.Services.AddSingleton<ISyncHealthPoller, NullSyncHealthPoller>();
builder.Services.AddScoped<IEdgeUpdateExecutor, SimulatedEdgeUpdateExecutor>();

// Mídia de produto (US-0xx) — upload de imagem do cardápio, autoridade da nuvem (CLAUDE.md,
// "cardápio é editado na nuvem"). Único registro desta lista que é implementação real invocada de
// verdade por um controller do Api.Cloud (ProductsController).
builder.Services.Configure<S3ProductMediaStorageOptions>(builder.Configuration.GetSection(S3ProductMediaStorageOptions.SectionName));
builder.Services.AddSingleton<IProductMediaStorage, S3ProductMediaStorage>();

// US-144 (Importação de cardápio por planilha) — leitura/geração de .xlsx via ClosedXML. Stateless,
// então AddSingleton (mesmo idioma de IProductMediaStorage acima).
builder.Services.AddSingleton<ISpreadsheetParser, ClosedXmlSpreadsheetParser>();

// IAuthorizationTokenValidator (US-004, gap "autorização pontual é só emitida, nunca validada") —
// valida o header X-Authorization-Token contra o que AuthorizeSensitiveActionCommandHandler emitiu;
// nenhum endpoint de negócio consome isto ainda (ver RequiresAuthorizationTokenAttribute), mas o
// mecanismo precisa estar pronto no DI para o próximo módulo que precisar de elevação pontual.
// IAuthSessionActivityGuard (US-004, gap "encerramento de sessão inativa configurável") — chamado
// por SessionActivityMiddleware a cada requisição autenticada.
builder.Services.AddScoped<IAuthorizationTokenValidator, AuthorizationTokenValidator>();
builder.Services.AddScoped<IAuthSessionActivityGuard, AuthSessionActivityGuard>();

// ISecretDigester: ver nota espelhada em Nexora.Api.Edge/Program.cs. No
// Api.Cloud, todo consumidor (RefreshToken, LoginWithPassword, ProvisionTenant,
// AcceptOwnerInvitation) digere segredo emitido pela própria nuvem (refresh
// token / token de instalação / token de convite) com o MESMO pepper
// Auth:Secrets:SecretPepper — confirmado lendo os quatro handlers. Por isso
// HmacSecretDigester (não DeviceSecretDigester, que é exclusiva do Api.Edge)
// é a única implementação registrada aqui.
builder.Services.AddSingleton<ISecretDigester, HmacSecretDigester>();

// US-145 (Acesso de suporte auditado) — ISupportAccessTokenValidator resolve/valida o token
// bruto de support_access (hash + IsActive) e registra o uso; nenhum controller desta tarefa
// consome um token bruto de suporte via header (grant/revoke/history/relatório autenticam via JWT
// PlatformAdmin/tenant, não via este token — ver relatório da tarefa), mas o serviço precisa estar
// pronto no DI para a próxima US que efetivamente ler dado de tenant "como suporte".
builder.Services.AddScoped<Nexora.Application.Platform.SupportAccessTokens.ISupportAccessTokenValidator,
    Nexora.Application.Platform.SupportAccessTokens.SupportAccessTokenValidator>();

builder.Services.AddSingleton<IEventOriginProvider, CloudEventOriginProvider>();
builder.Services.AddSingleton<IAppVersionProvider, AppVersionProvider>();

// ---------------------------------------------------------------------------
// Operação (US-020) — cadastro de ambientes/mesas e exportação de QR Codes. Autoridade do dado
// é a nuvem: só Api.Cloud registra estas dependências, o Api.Edge só lê a réplica sincronizada.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IQrTokenGenerator, QrTokenGenerator>();
builder.Services.AddSingleton<IQrCodePdfRenderer, TableQrCodesPdfRenderer>();

// ---------------------------------------------------------------------------
// Notificações / Storage — só usados por handlers do Api.Cloud (outbox de
// e-mail, backup recebido do edge, upload pré-assinado de mídia de marca).
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IEmailSender, EmailOutboxSender>();
builder.Services.AddSingleton<IBackupStorage, FileSystemBackupStorage>();
builder.Services.AddSingleton<IBrandingStorage, S3BrandingStorage>();

// EmailOutboxDeliveryWorker (US-002, gap "convite do gestor só enfileira e-mail, nunca envia de
// fato") — entrega efetiva de email_outbox. Sem "EmailOutbox:Smtp:Host" configurado (dev/CI),
// LoggingEmailDispatcher assume no lugar de um provedor SMTP real; o worker/mecanismo de entrega
// roda de ponta a ponta nos dois casos (só a "última milha" muda). Só Api.Cloud — convite de
// gestor é fluxo exclusivo de nuvem (ver EdgeInstallationOptions/EmailOutboxOptions, nunca
// configurados em Api.Edge).
var smtpHost = builder.Configuration[$"{EmailOutboxOptions.SectionName}:Smtp:Host"];
if (!string.IsNullOrWhiteSpace(smtpHost))
{
    builder.Services.AddSingleton<IEmailDispatcher, SmtpEmailDispatcher>();
}
else
{
    builder.Services.AddSingleton<IEmailDispatcher, LoggingEmailDispatcher>();
}

builder.Services.AddHostedService<EmailOutboxDeliveryWorker>();

// ---------------------------------------------------------------------------
// E-08 — motor de alertas e notificações. AlertRaiser é o núcleo único de
// criação/dedupe/direcionamento/agrupamento (US-080/US-082/US-083), usado tanto pelos handlers
// reativos (MarkProductUnavailable) quanto pelos workers de avaliação abaixo; registrado aqui
// (não em Infrastructure) porque é público na própria Application (ver docstring de AlertRaiser).
// Web Push (US-081 §2 "enviado pela nuvem") segue o MESMO padrão condicional de
// SmtpEmailDispatcher/LoggingEmailDispatcher acima: sem "WebPush:PublicKeyBase64Url" configurado
// (dev/CI), LoggingPushNotificationSender assume no lugar do envio real.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IAlertRaiser, AlertRaiser>();
builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection(WebPushOptions.SectionName));

var vapidPublicKey = builder.Configuration[$"{WebPushOptions.SectionName}:PublicKeyBase64Url"];
if (!string.IsNullOrWhiteSpace(vapidPublicKey))
{
    builder.Services.AddHttpClient<IPushNotificationSender, WebPushSender>();
}
else
{
    builder.Services.AddSingleton<IPushNotificationSender, LoggingPushNotificationSender>();
}

builder.Services.AddHostedService<AlertEvaluationWorker>();
builder.Services.AddHostedService<PushDeliveryWorker>();

// US-140 — painel de instalações com saúde: classificação OK/DEGRADED/DOWN é sempre da nuvem (§9),
// nunca dos eventos que o próprio edge emite sobre si mesmo. IPlatformAlertNotifier hoje só loga
// (mesmo idioma condicional de IEmailDispatcher acima); sem canal real configurado ainda, não há
// "if" a fazer aqui — só o registro do default de log.
builder.Services.AddSingleton<Nexora.Application.Abstractions.Notifications.IPlatformAlertNotifier, Nexora.Infrastructure.Notifications.LoggingPlatformAlertNotifier>();
builder.Services.AddHostedService<InstallationHealthEvaluationWorker>();

// ---------------------------------------------------------------------------
// US-143 — domínio próprio por cliente: verificação de posse por DNS TXT (real, DnsClient.NET —
// leitura pública sem efeito colateral, seguro rodar de verdade em qualquer ambiente), emissão de
// certificado TLS (ManualCertificateIssuer — dev-safe default; o cliente ACME/DNS-01 real é um
// próximo passo de infraestrutura, ver docstring da classe) e o redirecionamento do domínio padrão
// para o próprio (inerte sem "Platform:DefaultDomainSuffix" configurado).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDomainVerificationService, DnsClientDomainVerificationService>();
builder.Services.AddSingleton<ICertificateIssuer, ManualCertificateIssuer>();
builder.Services.Configure<PlatformDomainOptions>(builder.Configuration.GetSection(PlatformDomainOptions.SectionName));
builder.Services.AddScoped<ITenantDomainRedirectResolver, TenantDomainRedirectResolver>();
builder.Services.AddHostedService<TenantDomainCertificateRenewalWorker>();
// US-152 — resolução de links (público/admin) da visão 360 do estabelecimento; pura, sem I/O.
builder.Services.AddSingleton<IPlatformLinksResolver, PlatformLinksResolver>();

// ---------------------------------------------------------------------------
// Installations (cloud, plural) — registro/consumo de token de instalação,
// verificação de assinatura Ed25519 do edge, derivação de pepper de PIN por
// tenant e URL pública da nuvem para a resposta de registro.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IInstallationTokenDigester, InstallationTokenDigester>();
builder.Services.AddSingleton<IInstallationSignatureVerifier, InstallationSignatureVerifier>();
builder.Services.AddSingleton<IPinLookupPepperProvider, PinLookupPepperProvider>();
builder.Services.AddSingleton<ICloudEndpointOptions, CloudEndpointOptions>();

// ---------------------------------------------------------------------------
// Autenticação — dois esquemas: JWT Bearer (default, usuários) e "Installation"
// (assinatura Ed25519 do edge, ver InstallationAuthenticationHandler) usado
// explicitamente via [Authorize(AuthenticationSchemes = "Installation")] nas
// rotas de sync pull/health de Nexora.Api.Cloud.Controllers.InstallationsController.
// ---------------------------------------------------------------------------
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrEmpty(jwtOptions.Secret) ? new string('0', 32) : jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            // US-015: o navegador não anexa o header Authorization na conexão do hub SignalR — o
            // token vai via querystring ?access_token=..., só para o path do hub. Réplica idêntica
            // de Nexora.Api.Edge/Program.cs.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    })
    .AddScheme<AuthenticationSchemeOptions, InstallationAuthenticationHandler>("Installation", options => { });
builder.Services.AddAuthorization(options =>
{
    // Política de administração de plataforma (US-001, gap "TenantsController vaza dado
    // cross-tenant" — antes só exigia [Authorize] genérico, então qualquer usuário autenticado de
    // QUALQUER tenant conseguia listar/ler todos os estabelecimentos do parque).
    //
    // Claim dedicada "plt" (paralela a "tid"/"sid"/"did"/"ses" já lidas por
    // CloudCurrentTenantContext) em vez de reaproveitar "perms"/"tenant:manage" do catálogo de
    // Nexora.Domain.Platform.PermissionCatalog: aqueles códigos são RBAC de tenant — um dono de
    // estabelecimento (role OWNER) legitimamente tem "tenant:manage" para administrar A PRÓPRIA
    // loja, não para listar as dos outros. Reusar o mesmo código aqui reabriria exatamente o
    // vazamento cross-tenant que esta policy existe para fechar.
    //
    // PENDÊNCIA: nenhum emissor de token atual (JwtTokenIssuer/LoginWithPassword/LoginWithPin)
    // ainda sabe emitir "plt" — não existe hoje conceito de "operador da Replay" na base de
    // usuários (só app_user por tenant). Isso é deliberado: com a policy fechada, esta rota fica
    // inacessível a todo mundo até uma US futura modelar autenticação de equipe de plataforma —
    // estado seguro (nega por padrão), diferente do estado anterior (qualquer autenticado passava).
    options.AddPolicy("PlatformAdmin", policy => policy.RequireClaim("plt", "admin"));

    // Política de gestão de dispositivo (US-005, gap "DevicesController só tinha [Authorize]
    // genérico" — sem isto, qualquer usuário autenticado do tenant, inclusive um garçom, podia
    // renomear ou revogar qualquer terminal). Usa a claim "perms" já emitida por JwtTokenIssuer
    // (mesmo formato lido por CloudCurrentTenantContext.Permissions) — "device:manage",
    // "device:*" ou "*" no catálogo (Nexora.Domain.Platform.PermissionCatalog) satisfazem.
    // Listagem (GET) continua fora desta policy — qualquer autenticado do tenant pode consultar;
    // só escrita exige a permissão.
    options.AddPolicy("DeviceManage", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "device:manage")));

    // Política de escrita de identidade visual (US-003, gap "BrandingController só tinha
    // [Authorize] genérico" — sem isto, qualquer usuário autenticado do tenant, inclusive quem só
    // deveria consultar cardápio/relatório, podia alterar cor/logo/texto público do
    // estabelecimento). Mesmo padrão da policy "DeviceManage" acima — "config:write", "config:*"
    // ou "*" do catálogo (Nexora.Domain.Platform.PermissionCatalog) satisfazem. Leitura pública
    // (GET /public/branding, GET /tenant/branding.webmanifest) continua fora desta policy, como já
    // era (RN-016: identidade visual é dado público do estabelecimento) — só PATCH
    // /tenant/branding e POST /tenant/branding/logo exigem a permissão.
    options.AddPolicy("ConfigWrite", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "config:write")));

    // Políticas de catálogo de permissões e CRUD de papéis (US-004, gap "RBAC não verifica
    // permissão em nenhum endpoint de negócio" — RolesController só tinha [Authorize] genérico,
    // então qualquer usuário autenticado do tenant, inclusive um garçom, conseguia listar e
    // reescrever os papéis/permissões de qualquer um, inclusive o próprio OWNER). Mesmo padrão de
    // "DeviceManage"/"ConfigWrite" acima, usando o recurso "user" do catálogo
    // (Nexora.Domain.Platform.PermissionCatalog — "Equipe e acessos", já é o mesmo código que o
    // controller citava em TODO antes desta correção: "a versão TS exige user:read (listar) e
    // user:write (criar/editar)"). "user:read"/"user:write", "user:*" ou "*" satisfazem.
    options.AddPolicy("RoleRead", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "user:read")));

    options.AddPolicy("RoleWrite", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "user:write")));

    // Política de consulta da trilha de auditoria (E-09/US-091, RF-AUD-03 "acesso restrito a
    // perfis autorizados"). Mesmo padrão de "RoleRead"/"RoleWrite" acima — "audit:read", "audit:*"
    // ou "*" do catálogo (Nexora.Domain.Platform.PermissionCatalog) satisfazem.
    options.AddPolicy("AuditRead", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "audit:read")));

    // Política de gestão de ambientes/mesas do salão (US-020). Mesmo padrão de
    // "DeviceManage"/"ConfigWrite" acima, usando o recurso "table" do catálogo
    // (Nexora.Domain.Platform.PermissionCatalog) — "table:manage", "table:*" ou "*" satisfazem.
    // Listagem (GET) continua fora desta policy — qualquer autenticado do tenant pode consultar;
    // só escrita (criar/editar/ativar/desativar/excluir/rotacionar token/exportar PDF de QR) exige
    // a permissão, porque o PDF embute o qr_token (segredo de entrada da mesa).
    options.AddPolicy("TableManage", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "table:manage")));

    // Políticas de CRUD de cardápio — praças de produção, categorias e produtos (US-010/US-011/
    // US-017) compartilham o recurso "catalog" do catálogo de permissões
    // (Nexora.Domain.Platform.PermissionCatalog): "catalog:read" (listar) e "catalog:write"
    // (criar/editar/excluir). Mesmo nome do gêmeo em Nexora.Api.Edge/Program.cs.
    options.AddPolicy("StationRead", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:read")));

    options.AddPolicy("StationWrite", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:write")));

    options.AddPolicy("CategoryRead", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:read")));

    options.AddPolicy("CategoryWrite", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:write")));

    options.AddPolicy("ProductRead", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:read")));

    options.AddPolicy("ProductWrite", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:write")));

    // Disponibilidade operacional de produto (US-015) — mesma família de ProductRead/ProductWrite
    // acima, mas também satisfeita por "catalog:set_unavailable" (ação dedicada do catálogo de
    // permissões para marcar item indisponível sem conceder CRUD completo). Mesmo nome do gêmeo em
    // Nexora.Api.Edge/Program.cs.
    options.AddPolicy("ProductAvailability", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User
            .FindAll(PermissionAuthorization.PermissionClaimType)
            .Select(c => c.Value)
            .ToArray();
        return PermissionAuthorization.HasPermission(permissions, "catalog:set_unavailable")
               || PermissionAuthorization.HasPermission(permissions, "catalog:write");
    }));
});

// ---------------------------------------------------------------------------
// Observabilidade (ADR-022).
// ---------------------------------------------------------------------------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Nexora.Api.Cloud"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

// Geração de snapshot do OpenAPI para o CI (US-007, gap "snapshot de OpenAPI removido do CI" —
// o job antigo dependia de apps/api-cloud em Node, extinto). Não abre porta HTTP nem depende de
// Postgres/Redis reais: só materializa o Swagger/OpenAPI já registrado por AddSwaggerGen() acima a
// partir dos metadados de controller (Microsoft.AspNetCore.Mvc.ApiExplorer) e escreve em disco.
// Uso: `dotnet run --project backend/src/Nexora.Api.Cloud -- --generate-openapi-snapshot
// <arquivo-de-saida>` — ver infra/cloud/openapi/v1.snapshot.json, infra/scripts/
// openapi-compatibility.ts e o job "Contrato OpenAPI" de .github/workflows/pull-request.yml.
var openApiSnapshotArgIndex = Array.IndexOf(args, "--generate-openapi-snapshot");
if (openApiSnapshotArgIndex >= 0)
{
    var openApiSnapshotPath = openApiSnapshotArgIndex + 1 < args.Length
        ? args[openApiSnapshotArgIndex + 1]
        : "openapi.snapshot.json";

    var swaggerProvider = app.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();
    var openApiDocument = swaggerProvider.GetSwagger("v1");

    var openApiSnapshotDirectory = Path.GetDirectoryName(Path.GetFullPath(openApiSnapshotPath));
    if (!string.IsNullOrEmpty(openApiSnapshotDirectory))
    {
        Directory.CreateDirectory(openApiSnapshotDirectory);
    }

    await using var openApiSnapshotStream = File.Create(openApiSnapshotPath);
    await using var openApiSnapshotWriter = new StreamWriter(openApiSnapshotStream);
    openApiDocument.SerializeAsV3(new Microsoft.OpenApi.Writers.OpenApiJsonWriter(openApiSnapshotWriter));
    await openApiSnapshotWriter.FlushAsync();

    return;
}

app.UseExceptionHandler(); // ProblemDetails (ADR-021) para exceção não mapeada -> INTERNAL_ERROR sem stack trace.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.MapGet("/v1/dev/auth/otp", async (
        IApplicationDbContext db,
        IMfaSecretCipher mfaCipher,
        CancellationToken cancellationToken) =>
    {
        const string adminEmail = "plataforma@nexora.local";

        var lookup = await db.FindLoginCredentialByEmailAsync(adminEmail, cancellationToken);
        if (lookup is null)
        {
            return Results.NotFound();
        }

        await db.SetTenantContextAsync(lookup.TenantId, cancellationToken);

        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == lookup.UserId && u.TenantId == lookup.TenantId && u.DeletedAt == null, cancellationToken);

        if (user is null || user.MfaSecret is null)
        {
            return Results.NotFound();
        }

        var secret = mfaCipher.Decrypt(user.MfaSecret);
        var otp = TotpCodeGenerator.Current(secret);
        var expiresInSeconds = TotpCodeGenerator.SecondsRemaining();

        return Results.Ok(new { otp, expiresInSeconds });
    })
    .AllowAnonymous()
    .ExcludeFromDescription();
}

// US-143 §3.1 "Redirecionamento do domínio padrão para o próprio" — antes de UseRouting() de
// propósito: não depende de endpoint resolvido nem de autenticação, só do Host da requisição
// (GET). Inerte (nunca redireciona) sem "Platform:DefaultDomainSuffix" configurado.
app.UseMiddleware<Nexora.Api.Cloud.Infrastructure.Platform.TenantDomainRedirectMiddleware>();

// UseRouting() explícito (em vez de confiar na inserção implícita do WebApplication) para que a
// ordem abaixo seja inequívoca: autenticação/autorização e os middlewares seguintes precisam do
// endpoint já resolvido (IdempotencyMiddleware lê [IdempotencyExempt] via HttpContext.GetEndpoint()).
app.UseRouting();
app.UseAuthentication();
// US-004: sessão sem atividade recente (TenantConfig.Operation.sessionInactivityMinutes) nega
// ANTES de qualquer policy de permissão avaliar — depois de UseAuthentication() (precisa da claim
// "ses" já resolvida por CloudCurrentTenantContext), antes de UseAuthorization().
app.UseMiddleware<SessionActivityMiddleware>();
app.UseAuthorization();
// ADR-022: tenant.id/store.id/device.id/user.id como tags do Activity corrente, antes de qualquer
// log/span downstream do handler.
app.UseMiddleware<ActivityEnrichmentMiddleware>();
// ADR-020: Idempotency-Key obrigatório em POST/PUT/PATCH/DELETE (exceto [IdempotencyExempt]) —
// precisa rodar depois da autenticação (para conhecer o tenant) e depois do routing (para ler o
// metadado de isenção do endpoint), mas antes do controller processar a requisição de verdade.
app.UseMiddleware<IdempotencyMiddleware>();
app.MapControllers();
app.MapHub<CatalogAvailabilityHub>("/hubs/catalog-availability").WithMetadata(new IdempotencyExemptAttribute());

app.Run();
