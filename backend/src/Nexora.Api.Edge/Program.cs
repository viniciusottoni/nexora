using System.Text;
using Nexora.Api.Edge.Hubs;
using Nexora.Api.Edge.Infrastructure;
using Nexora.Api.Edge.Infrastructure.Auth;
using Nexora.Api.Edge.Infrastructure.Idempotency;
using Nexora.Api.Edge.Infrastructure.Observability;
using Nexora.Api.Edge.Realtime;
using Nexora.Api.Edge.Workers;
using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Idempotency;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Auth.Shared;
using Nexora.Application.Devices.Abstractions;
using Nexora.Application.Installation.Abstractions;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Http;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Idempotency;
using Nexora.Infrastructure.Installation;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Interceptors;
using Nexora.Infrastructure.Platform;
using Nexora.Shared.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Persistência (ADR-038): AppDbContext + TenantConnectionInterceptor (RLS via
// SET LOCAL app.tenant_id). TenantConnectionInterceptor e ICurrentTenantContext
// são Scoped de propósito — precisam variar por requisição/transação; o `sp`
// recebido pelo callback de AddDbContext já é resolvido no escopo da própria
// instância de AppDbContext sendo criada (garantia do EF Core), então o
// interceptor sempre enxerga o tenant certo da requisição corrente.
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
// CQRS/MediatR (ADR-037) — pipeline: Validation -> Logging -> Transaction.
// Um único assembly de Application nesta solution (não há módulos separados
// por assembly), então o registro cobre todos os handlers; cada Api só expõe
// os que seus controllers efetivamente despacham.
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
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // ADR-017: dinheiro nunca é double/float — decimal no C#, string no JSON. Registrado
    // globalmente (não por DTO) para que nenhum campo monetário futuro esqueça o converter
    // (mesma classe já usada pontualmente em ModifierResponse — US-023 é o primeiro endpoint a
    // precisar dela em toda resposta, o que expôs a lacuna de registro global documentada ali).
    options.JsonSerializerOptions.Converters.Add(new MoneyJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableMoneyJsonConverter());
});
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
// Options (IOptions<T>) — uma Configure<T> por classe de opções consumida
// pelas implementações concretas registradas abaixo.
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AuthSecretsOptions>(builder.Configuration.GetSection(AuthSecretsOptions.SectionName));
builder.Services.Configure<DeviceSecurityOptions>(builder.Configuration.GetSection(DeviceSecurityOptions.SectionName));
// EdgeInstallationOptions (Api.Edge, só TenantId) e EdgeInstallationIdentityOptions
// (Infrastructure, InstallationId/PrivateKeyPath/SyncEndpoint/SyncHealthIntervalMs)
// deliberadamente compartilham a mesma seção "Edge:Installation" — são duas visões
// (autenticação local vs. identidade de sincronização) do mesmo bloco de config.
builder.Services.Configure<EdgeInstallationOptions>(builder.Configuration.GetSection(EdgeInstallationOptions.SectionName));
builder.Services.Configure<EdgeInstallationIdentityOptions>(builder.Configuration.GetSection(EdgeInstallationIdentityOptions.SectionName));
builder.Services.Configure<AppVersionOptions>(builder.Configuration.GetSection(AppVersionOptions.SectionName));
builder.Services.Configure<RedisHealthCheckOptions>(builder.Configuration.GetSection(RedisHealthCheckOptions.SectionName));

// ---------------------------------------------------------------------------
// Segurança / Auth — só os consumidores que o Api.Edge realmente expõe (login
// por PIN, autorização pontual por PIN de gerente, pareamento de dispositivo).
// ICredentialHasher/IPinLookupDigester/ITokenIssuer são usados por
// LoginWithPinCommandHandler e AuthorizeSensitiveActionCommandHandler.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<ICredentialHasher, Argon2CredentialHasher>();
builder.Services.AddSingleton<IPinLookupDigester, HmacPinLookupDigester>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<ICurrentTenantContext, EdgeCurrentTenantContext>();

// IAuthorizationTokenValidator (US-004, gap "autorização pontual é só emitida, nunca validada") —
// valida o header X-Authorization-Token contra o que AuthorizeSensitiveActionCommandHandler emitiu;
// nenhum endpoint de negócio consome isto ainda (ver RequiresAuthorizationTokenAttribute), mas o
// mecanismo precisa estar pronto no DI para o próximo módulo que precisar de elevação pontual.
// IAuthSessionActivityGuard (US-004, gap "encerramento de sessão inativa configurável") — chamado
// por SessionActivityMiddleware a cada requisição autenticada.
builder.Services.AddScoped<IAuthorizationTokenValidator, AuthorizationTokenValidator>();
builder.Services.AddScoped<IAuthSessionActivityGuard, AuthSessionActivityGuard>();

// ISecretDigester: registro DELIBERADAMENTE de escopo por processo (ver ATENÇÃO
// em Nexora.Infrastructure.Devices.DeviceSecretDigester). Existem duas
// implementações concorrentes em Infrastructure — DeviceSecretDigester (pepper
// de dispositivo) e Auth.HmacSecretDigester (pepper de refresh token) — mas
// nenhum handler despachado pelo Api.Edge precisa da segunda: LoginWithPin,
// PairDevice e CreatePairingCode usam ISecretDigester só para segredo de
// dispositivo/código de pareamento (confirmado lendo os três handlers). O
// Api.Cloud registra a implementação HmacSecretDigester (ver seu Program.cs).
// Registrar as duas implementações no mesmo container faria a última
// sobrescrever a outra silenciosamente — por isso cada Api regista só a sua.
builder.Services.AddSingleton<ISecretDigester, DeviceSecretDigester>();

builder.Services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
builder.Services.AddSingleton<IAppVersionProvider, AppVersionProvider>();

// ---------------------------------------------------------------------------
// Devices — geração de código de pareamento e de segredo do dispositivo.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IPairingCodeGenerator, PairingCodeGenerator>();
builder.Services.AddSingleton<IDeviceSecretGenerator, DeviceSecretGenerator>();

// ---------------------------------------------------------------------------
// Installation (edge, singular) — bootstrap de primeira subida e saúde local.
// Bootstrap de catálogo/autorização ainda não tem módulo real (ver notas nos
// próprios arquivos Null*Importer) — mantém ImportBootstrapCommand funcional
// de ponta a ponta enquanto esses módulos não existem.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IBootstrapCatalogImporter, NullBootstrapCatalogImporter>();
builder.Services.AddSingleton<IBootstrapAuthorizationImporter, NullBootstrapAuthorizationImporter>();
builder.Services.AddSingleton<IRedisHealthChecker, RedisHealthChecker>();
builder.Services.AddSingleton<IInstallationRequestSigner, InstallationRequestSigner>();
builder.Services.AddHttpClient<ISyncHealthPoller, SyncHealthPoller>();

// SyncOutboxWorker (BackgroundService) — só no edge; dispara PollSyncHealthCommand
// em intervalo configurável, resolvendo ISender por escopo a cada tick.
builder.Services.AddHostedService<SyncOutboxWorker>();

// ---------------------------------------------------------------------------
// SignalR (US-015) — propagação em tempo real de product.unavailable/product.available na LAN da
// loja (mesa, garçom, KDS). Réplica idêntica de Nexora.Api.Cloud/Program.cs.
// ---------------------------------------------------------------------------
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    // Mesmo converter de dinheiro do AddJsonOptions acima (ADR-017) — o mapa de mesas (US-023)
    // manda valor consumido pelas mensagens do hub, não só pela resposta REST.
    options.PayloadSerializerOptions.Converters.Add(new MoneyJsonConverter());
    options.PayloadSerializerOptions.Converters.Add(new NullableMoneyJsonConverter());
});
builder.Services.AddSingleton<IAvailabilityBroadcaster, SignalRAvailabilityBroadcaster>();
builder.Services.AddHostedService<AvailabilityAutoRestoreWorker>();

// SignalR (US-024) — consumo da mesa em tempo real, réplica do mesmo padrão acima (ADR-011).
builder.Services.AddSingleton<IOrderConsumptionBroadcaster, SignalRTableConsumptionBroadcaster>();

// SignalR (US-023) — mapa de mesas em tempo real, réplica do mesmo padrão acima (ADR-011).
builder.Services.AddSingleton<ITableMapBroadcaster, SignalRTableMapBroadcaster>();

// SignalR (US-025/US-026) — alerta dirigido a papel/pessoa (chamar garçom, pedir a conta), réplica
// do mesmo padrão acima, salas role:{papel}/user:{id} em vez de tenant (ver Hubs.AlertsHub).
builder.Services.AddSingleton<IAlertsBroadcaster, SignalRAlertsBroadcaster>();
builder.Services.AddHostedService<WaiterCallEscalationWorker>();

// E-08 — motor de alertas do MVP (US-080/US-082/US-083). AlertRaiser é o núcleo único de
// criação/dedupe/direcionamento/agrupamento, público na própria Application (ver docstring de
// AlertRaiser) para ser instanciável por AddScoped a partir daqui.
builder.Services.AddScoped<IAlertRaiser, AlertRaiser>();
builder.Services.AddHostedService<AlertEvaluationWorker>();

// SignalR (US-031) — roteamento de pedido/item por praça (ADR-011), salas station:{id}/
// role:{papel}/table:{id} (ver Hubs.KdsHub).
builder.Services.AddSingleton<IStationBroadcaster, SignalRStationBroadcaster>();

// SignalR (US-034) — estado de conectividade edge<->nuvem (sync.status) para todos os
// dispositivos da loja, sala tenant:{id} (ADR-011, ver Hubs.SyncStatusHub).
builder.Services.AddSingleton<ISyncStatusBroadcaster, SignalRSyncStatusBroadcaster>();

// ---------------------------------------------------------------------------
// Autenticação — JWT Bearer (ADR-037/doc. 05). Mesmo formato de claims/segredo
// simétrico usado por JwtTokenIssuer (ver ClockSkew alinhado: 30s).
// ---------------------------------------------------------------------------
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
    string.IsNullOrEmpty(jwtOptions.Secret) ? new string('0', 32) : jwtOptions.Secret));

// US-021 §3.1: "escopo mínimo" — o token anônimo de sessão de mesa (JwtTokenIssuer,
// tokenUse="table_session") nunca deve autenticar como staff, mesmo por acidente, contra um
// endpoint com [Authorize] simples e sem policy de permissão (que aceitaria QUALQUER usuário
// autenticado do esquema padrão). Por isso ele vive num esquema de autenticação SEPARADO
// ("TableSession"): [Authorize] sem esquema explícito só tenta o esquema PADRÃO
// (JwtBearerDefaults.AuthenticationScheme, definido abaixo em AddAuthentication), então um token
// de sessão de mesa jamais chega a ser avaliado pelas policies de staff — e o esquema padrão
// também rejeita explicitamente qualquer token com tokenUse="table_session" (defesa em
// profundidade, caso um endpoint futuro decida aceitar os dois esquemas ao mesmo tempo).
const string TableSessionScheme = "TableSession";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isto, o handler remapeia claims curtas conhecidas para URIs XML/WS-* legadas antes
        // de qualquer código nosso ver o ClaimsPrincipal — "tid" vira
        // "http://schemas.microsoft.com/identity/claims/tenantid" e "roles" vira
        // ".../ws/2008/06/identity/claims/role" (confirmado empiricamente construindo
        // TableMapHubTests, US-023: sem este flag, TableMapHub/CatalogAvailabilityHub nunca leem
        // "tid" e nenhuma mesa jamais chega ao grupo de tenant certo). Bug pré-existente e mais
        // amplo do que esta US: EdgeCurrentTenantContext.Roles (ReadListClaim("roles")) sofria o
        // mesmo problema e devolvia sempre lista vazia em qualquer requisição autenticada.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSigningKey,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            // US-015: o navegador nativo (WebSocket/EventSource) não consegue anexar o header
            // Authorization na conexão do hub SignalR — o cliente (web-kds) manda o JWT como
            // querystring ?access_token=..., só para o path do hub (rotas REST continuam exigindo
            // o header Authorization de verdade). Mesma configuração espelhada em
            // Nexora.Api.Cloud/Program.cs.
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
            // Defesa em profundidade (ver comentário de TableSessionScheme acima): um token de
            // sessão de mesa jamais autentica com sucesso no esquema padrão (staff), mesmo que
            // assinatura/issuer/audience/validade estejam corretos.
            OnTokenValidated = context =>
            {
                if (context.Principal?.FindFirst("tokenUse")?.Value == "table_session")
                {
                    context.Fail("Token de sessão de mesa não é aceito neste esquema.");
                }

                return Task.CompletedTask;
            },
        };
    })
    .AddJwtBearer(TableSessionScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSigningKey,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            // US-024: o TableConsumptionHub também precisa do token via querystring (mesmo motivo
            // do CatalogAvailabilityHub/US-015 — o navegador não anexa header Authorization na
            // conexão do hub). Réplica do mesmo bloco do esquema padrão acima, só restrito ao path
            // deste hub.
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
            // Só o token emitido por JwtTokenIssuer.IssueTableSessionTokenAsync passa aqui —
            // um access token de staff (tokenUse="access") é rejeitado mesmo com assinatura válida.
            OnTokenValidated = context =>
            {
                if (context.Principal?.FindFirst("tokenUse")?.Value != "table_session")
                {
                    context.Fail("Este endpoint exige um token de sessão de mesa.");
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Política de gestão de dispositivo (US-005, gap "DevicesController só tinha [Authorize]
    // genérico" — sem isto, qualquer usuário autenticado do tenant, inclusive um garçom, podia
    // gerar código de pareamento, renomear ou revogar qualquer terminal). Usa a claim "perms" já
    // emitida por JwtTokenIssuer (mesmo formato lido por EdgeCurrentTenantContext.Permissions) —
    // "device:manage", "device:*" ou "*" no catálogo (Nexora.Domain.Platform.PermissionCatalog)
    // satisfazem. Listagem (GET) continua fora desta policy — qualquer autenticado do tenant pode
    // consultar; só escrita exige a permissão.
    options.AddPolicy("DeviceManage", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "device:manage")));

    // Políticas de leitura/escrita de disponibilidade de produto (US-015) — mesmo recurso "catalog"
    // do catálogo de permissões que US-010/US-011 usam para CRUD de categorias/produtos/variantes
    // na nuvem. Registradas aqui (edge) pela primeira vez porque, antes desta história, o Api.Edge
    // não expunha nenhum endpoint de catálogo (cardápio é "editado na nuvem, só lido no local" —
    // esta é a exceção bidirecional da US-015). Nome igual ao do gêmeo em Nexora.Api.Cloud/Program.cs.
    options.AddPolicy("ProductRead", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:read")));

    options.AddPolicy("ProductWrite", policy => policy.RequireAssertion(context =>
        PermissionAuthorization.HasPermission(
            context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value),
            "catalog:write")));

    options.AddPolicy("ProductAvailability", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User
            .FindAll(PermissionAuthorization.PermissionClaimType)
            .Select(c => c.Value)
            .ToArray();
        return PermissionAuthorization.HasPermission(permissions, "catalog:set_unavailable")
               || PermissionAuthorization.HasPermission(permissions, "catalog:write");
    }));

    // Abertura/alteração de sessão de mesa (US-022) — "table:open"/"table:transfer"/"table:manage"
    // já existem no catálogo fechado (Nexora.Domain.Platform.PermissionCatalog) desde antes desta
    // história; "table:manage" também satisfaz porque quem administra mesas por completo pode
    // abri-las. Distinta de "TableManage" (Nexora.Api.Cloud/Program.cs): aquela é CRUD de
    // dining_table em si (autoridade da nuvem); esta é abrir/alterar a SESSÃO (autoridade local).
    options.AddPolicy("TableOpen", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "table:open")
               || PermissionAuthorization.HasPermission(permissions, "table:manage");
    }));

    options.AddPolicy("TableRead", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "table:read")
               || PermissionAuthorization.HasPermission(permissions, "table:open")
               || PermissionAuthorization.HasPermission(permissions, "table:manage");
    }));

    // Token anônimo de sessão de mesa (US-021 §3.1) — exige o esquema "TableSession" (nunca o
    // padrão de staff, ver comentário de TableSessionScheme acima). Sem exigir nenhuma permissão
    // (o cliente final não tem papel/permissão nenhuma) — só o esquema já restringe o escopo do
    // token. Endpoints futuros (chamar garçom, pedir a conta, ver consumo — fora desta wave) que
    // usarem esta policy devem também comparar a claim "ses"/"tbl" do token com o id da rota, para
    // impedir que o token de uma mesa acesse outra (RN-015) — este projeto ainda não expõe nenhum
    // desses endpoints; a policy só fixa o esquema de autenticação aceito.
    options.AddPolicy("SessionScope", policy => policy
        .AddAuthenticationSchemes(TableSessionScheme)
        .RequireAuthenticatedUser());

    // Lançamento de item de pedido (US-024/US-028, gap de US-030 — ver docstring de
    // AddOrderItemCommandHandler) — "order:add_item" já existe no catálogo fechado
    // (Nexora.Domain.Platform.PermissionCatalog) desde antes desta história.
    options.AddPolicy("OrderAddItem", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "order:add_item");
    }));

    // Avanço mínimo de status de item (US-024, gap de KDS — ver docstring de
    // AdvanceOrderItemStatusCommandHandler) — "kds:advance" já existe no catálogo fechado.
    options.AddPolicy("KdsAdvance", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "kds:advance");
    }));

    // Leitura da fila do KDS (US-031, GET /v1/kds/queue) — "kds:read" ou "kds:advance" (quem já
    // pode avançar item plausivelmente também pode só olhar a fila) satisfazem.
    options.AddPolicy("KdsQueueRead", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "kds:read")
               || PermissionAuthorization.HasPermission(permissions, "kds:advance");
    }));

    // Criação de pedido pelo garçom/POS (US-030 §7, POST /v1/orders) — "order:create" já existe
    // no catálogo fechado (Nexora.Domain.Platform.PermissionCatalog); o caminho público (cliente
    // via QR) usa a policy "SessionScope" já existente, nunca esta.
    options.AddPolicy("OrderCreate", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "order:create");
    }));

    // Leitura de pedido (US-030 §7, GET /v1/orders/{id}) — "order:read"/"kds:read"/"report:read"
    // cobrem os papéis que plausivelmente consultam um pedido isolado, mesmo raciocínio de
    // "OrderItemTimelineRead" (US-032).
    options.AddPolicy("OrderRead", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "order:read")
               || PermissionAuthorization.HasPermission(permissions, "kds:read")
               || PermissionAuthorization.HasPermission(permissions, "report:read");
    }));

    // Timeline de carimbos de um item (US-032 §7 — drill-down do painel, RF-BI-11) — leitura, não
    // exige a permissão de AVANÇAR o KDS: "order:read"/"kds:read"/"report:read" já existem no
    // catálogo fechado (Nexora.Domain.Platform.PermissionCatalog) e cobrem os três papéis que
    // plausivelmente consultam isto (quem administra pedido, quem opera o KDS, quem lê relatório).
    options.AddPolicy("OrderItemTimelineRead", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "order:read")
               || PermissionAuthorization.HasPermission(permissions, "kds:read")
               || PermissionAuthorization.HasPermission(permissions, "report:read");
    }));

    // Divisão da conta (US-027) — "table:close_request" já existe no catálogo fechado desde antes
    // desta história (descrição "Solicitar fechamento") e nunca tinha sido usado por nenhuma
    // policy real: é exatamente o gesto de "preparar o fechamento da comanda" que assign-items/
    // waive-service-fee representam (cálculo/preview, nenhum fato de negócio persistido) —
    // "table:manage" também satisfaz, mesmo raciocínio de TableOpen/TableRead acima.
    options.AddPolicy("BillManage", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "table:close_request")
               || PermissionAuthorization.HasPermission(permissions, "table:manage");
    }));

    // Registro de pagamento parcial (US-027 §4, cenário "Divisão por valor") — "payment:register"
    // já existe no catálogo fechado (Nexora.Domain.Platform.PermissionCatalog), reservado para o
    // caixa registrar recebimentos.
    options.AddPolicy("PaymentRegister", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "payment:register");
    }));

    // Cancelamento de item/pedido (US-033) — só a permissão BASE de tentar cancelar
    // ("order:cancel_queued", item ainda na fila; "order:cancel_started" também satisfaz, papéis
    // com a permissão mais forte não precisam da mais fraca). A elevação para item JÁ INICIADO é
    // verificada dentro do handler via X-Authorization-Token (ADR-023) — não pode ser uma policy
    // estática porque depende do ESTADO do item/pedido no momento da chamada, não do papel de
    // quem chama.
    options.AddPolicy("OrderCancelItem", policy => policy.RequireAssertion(context =>
    {
        var permissions = context.User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
        return PermissionAuthorization.HasPermission(permissions, "order:cancel_queued")
               || PermissionAuthorization.HasPermission(permissions, "order:cancel_started");
    }));
});

// ---------------------------------------------------------------------------
// Observabilidade (ADR-022) — pacotes já referenciados no .csproj; exporter
// OTLP aponta para o coletor local do parque edge quando configurado, sem
// travar o boot se não houver coletor rodando (falha assíncrona/silenciosa,
// comportamento padrão do SDK OpenTelemetry).
// ---------------------------------------------------------------------------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Nexora.Api.Edge"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// ValidateOnBuild (ligado por padrão só em Development) validaria eagerly TODO handler MediatR
// registrado a partir do assembly compartilhado Nexora.Application — inclusive os que pertencem
// só ao Api.Cloud (IEmailSender, IOtpVerifier, IBackupStorage etc.) e que este processo nunca
// despacha. Desligar aqui reproduz o comportamento que este host já tem em produção (onde
// ValidateOnBuild é false por padrão), sem esconder nenhum problema novo.
builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);

var app = builder.Build();

// ---------------------------------------------------------------------------
// Modo "--migrate" (US-006, gap P0-1): aplica as migrations do EF Core
// (Nexora.Infrastructure/Persistence/Migrations) contra o Postgres do compose e
// encerra sem abrir porta HTTP — substitui, na MESMA imagem do api-edge, o antigo
// serviço `migrator` que rodava `pnpm --filter @db/db prisma:deploy` (stack Node
// morta). Usado só pelo serviço `migrator` de infra/edge/docker-compose.yml
// (command: ["dotnet", "Nexora.Api.Edge.dll", "--migrate"]); o serviço `api-edge`
// sobe a imagem normalmente, sem esse argumento.
// ---------------------------------------------------------------------------
if (args.Contains("--migrate"))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.UseExceptionHandler(); // ProblemDetails (ADR-021) para exceção não mapeada -> INTERNAL_ERROR sem stack trace.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// UseRouting() explícito (em vez de confiar na inserção implícita do WebApplication) para que a
// ordem abaixo seja inequívoca: autenticação/autorização e os middlewares seguintes precisam do
// endpoint já resolvido (IdempotencyMiddleware lê [IdempotencyExempt] via HttpContext.GetEndpoint()).
app.UseRouting();
app.UseAuthentication();
// US-004: sessão sem atividade recente (TenantConfig.Operation.sessionInactivityMinutes) nega
// ANTES de qualquer policy de permissão avaliar — depois de UseAuthentication() (precisa da claim
// "ses" já resolvida por EdgeCurrentTenantContext), antes de UseAuthorization().
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

// US-015: hub fino, só broadcast servidor->cliente. Réplica idêntica em
// Nexora.Api.Cloud/Program.cs no mesmo path relativo.
// O handshake do SignalR (negotiate + fallback de long-polling) usa POST sem cabeçalho
// Idempotency-Key nenhum — sem esta isenção, IdempotencyMiddleware (ADR-020) rejeitaria toda
// conexão a qualquer hub com 422 IDEMPOTENCY_KEY_REQUIRED. Não é "isento porque é infraestrutura
// de transporte, não uma escrita de negócio duplicável" — o mesmo raciocínio do login por PIN,
// documentado em IdempotencyExemptAttribute (US-023 encontrou isto construindo TableMapHub e a
// correção vale igualmente para os hubs já existentes).
app.MapHub<CatalogAvailabilityHub>("/hubs/catalog-availability").WithMetadata(new IdempotencyExemptAttribute());

// US-024: consumo da mesa em tempo real, esquema TableSession (ver TableConsumptionHub).
app.MapHub<TableConsumptionHub>("/hubs/table-consumption").WithMetadata(new IdempotencyExemptAttribute());

// US-023: mapa de mesas em tempo real (ADR-011), esquema padrão + policy TableRead.
app.MapHub<TableMapHub>("/hubs/table-map").WithMetadata(new IdempotencyExemptAttribute());

// US-025/US-026: alerta dirigido a papel/pessoa (chamar garçom, pedir a conta), esquema padrão —
// só staff se conecta (ver docstring de AlertsHub).
app.MapHub<AlertsHub>("/hubs/alerts").WithMetadata(new IdempotencyExemptAttribute());

// US-031: roteamento de pedido/item por praça (ADR-011), esquema padrão — só staff (KDS, caixa,
// garçom) se conecta (ver docstring de KdsHub).
app.MapHub<KdsHub>("/hubs/kds").WithMetadata(new IdempotencyExemptAttribute());

// US-034: estado de conectividade edge<->nuvem (sync.status) para todos os dispositivos da loja
// (ADR-011), esquema padrão (ver docstring de SyncStatusHub).
app.MapHub<SyncStatusHub>("/hubs/sync-status").WithMetadata(new IdempotencyExemptAttribute());

app.Run();

// Exposto para Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory<Program>) — Nexora.ApiTests
// precisa de um tipo público para o host de teste; instruções top-level geram uma classe Program
// implícita e internal, que WebApplicationFactory<T> não consegue referenciar de outro assembly.
public partial class Program
{
}
