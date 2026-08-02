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
using Nexora.Application.Auth.Shared;
using Nexora.Application.Devices.Abstractions;
using Nexora.Application.Installation.Abstractions;
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
    options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>());
});
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<TenantConnectionInterceptor>();

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
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAvailabilityBroadcaster, SignalRAvailabilityBroadcaster>();
builder.Services.AddHostedService<AvailabilityAutoRestoreWorker>();

// ---------------------------------------------------------------------------
// Autenticação — JWT Bearer (ADR-037/doc. 05). Mesmo formato de claims/segredo
// simétrico usado por JwtTokenIssuer (ver ClockSkew alinhado: 30s).
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
        // US-015: o navegador nativo (WebSocket/EventSource) não consegue anexar o header
        // Authorization na conexão do hub SignalR — o cliente (web-kds) manda o JWT como
        // querystring ?access_token=..., só para o path do hub (rotas REST continuam exigindo o
        // header Authorization de verdade). Mesma configuração espelhada em Nexora.Api.Cloud/Program.cs.
        options.Events = new JwtBearerEvents
        {
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
app.MapHub<CatalogAvailabilityHub>("/hubs/catalog-availability");

app.Run();
