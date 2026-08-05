using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Auth.Shared;
using Nexora.Application.Installations.Abstractions;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Catalog;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Installations;
using Nexora.Infrastructure.Notifications;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Platform;
using Nexora.IntegrationTests.Fakes;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nexora.IntegrationTests.Fixtures;

/// <summary>
/// Monta o mesmo pipeline MediatR de produção (Validation -&gt; Logging -&gt; Transaction, ADR-037)
/// contra um <see cref="IApplicationDbContext"/> real (Postgres via <see cref="PostgresFixture"/>) —
/// generalização do helper privado que <c>CrossTenantAuditTests</c> (US-001) já usava, agora
/// compartilhado pelos testes de provisionamento/instalação da US-002. Registra as implementações
/// reais de Infrastructure para os poucos serviços que os handlers de Tenants/Installations
/// precisam (nenhum mock de infraestrutura — só segredos de teste, nunca de produção).
/// </summary>
internal static class MediatRTestContainerFactory
{
    private const string TestSecretPepper = "integration-test-secret-pepper-nao-e-producao";
    private const string TestEmailEncryptionKey = "integration-test-email-encryption-key-nao-e-producao";
    private const string TestMfaEncryptionKey = "integration-test-mfa-encryption-key-32-bytes!!";
    private const string TestJwtSecret = "integration-test-jwt-secret-com-pelo-menos-32-bytes";

    public static ServiceProvider Build(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IAlertsBroadcaster? alertsBroadcaster = null,
        IAvailabilityBroadcaster? availabilityBroadcaster = null,
        ITableMapBroadcaster? tableMapBroadcaster = null,
        IStationBroadcaster? stationBroadcaster = null,
        IDomainVerificationService? domainVerificationService = null,
        ICertificateIssuer? certificateIssuer = null,
        IPlatformAlertNotifier? platformAlertNotifier = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();

        // US-025/US-026: CallWaiterCommand/AcknowledgeWaiterCallCommand/RequestBillCommand/
        // RequestBillByQrCommand dependem destes dois broadcasters — duplos de gravação por
        // padrão (o chamador passa a MESMA instância quando quiser inspecionar as chamadas).
        services.AddSingleton(alertsBroadcaster ?? new RecordingAlertsBroadcaster());
        services.AddSingleton(availabilityBroadcaster ?? new RecordingAvailabilityBroadcaster());
        services.AddSingleton(tableMapBroadcaster ?? new RecordingTableMapBroadcaster());
        // AddOrderItemCommand (gap de US-030, reaproveitado pelo cenário "Novo pedido após
        // solicitar a conta" da US-026) também depende de IOrderConsumptionBroadcaster.
        services.AddSingleton<IOrderConsumptionBroadcaster>(new RecordingOrderConsumptionBroadcaster());
        // US-015/US-044: handlers de disponibilidade publicam SignalR de forma síncrona; no
        // container de integração usamos o mesmo duplo determinístico das suites dedicadas.
        services.AddSingleton<IAvailabilityBroadcaster>(new RecordingAvailabilityBroadcaster());
        // US-031 (Roteamento simultâneo para cozinha e caixa): CreateOrderCommand/AddOrderItemCommand/
        // AdvanceOrderItemStatusCommand também dependem de IStationBroadcaster — sem este registro,
        // TODO teste que despacha esses três comandos por este factory quebraria a resolução de DI.
        services.AddSingleton(stationBroadcaster ?? new RecordingStationBroadcaster());
        // US-030 §8: geração real de short_code (AddOrderItemCommand/CreateOrderCommand) — precisa
        // do AppDbContext concreto (Npgsql cru, ADR-039), nunca só a porta IApplicationDbContext.
        if (db is AppDbContext appDbContext)
        {
            services.AddSingleton<IOrderShortCodeAllocator>(new OrderShortCodeAllocator(appDbContext));
        }

        var authSecrets = Options.Create(new AuthSecretsOptions
        {
            SecretPepper = TestSecretPepper,
            PinLookupPepper = TestSecretPepper,
            MfaEncryptionKey = TestMfaEncryptionKey,
        });
        services.AddSingleton<ISecretDigester>(new HmacSecretDigester(authSecrets));
        services.AddSingleton<IInstallationTokenDigester, InstallationTokenDigester>();
        services.AddSingleton(Options.Create(new EmailOutboxOptions { EncryptionKey = TestEmailEncryptionKey }));
        services.AddSingleton<IEmailSender, EmailOutboxSender>();

        // Auth (US-004): registrado aqui, não só nos handlers de Tenants/Installations, para que
        // testes de LoginWithPassword/LoginWithPin/AuthorizeSensitiveAction usem o MESMO container
        // MediatR de produção em vez de instanciar handlers isolados com dependências manuais.
        services.AddSingleton(authSecrets);
        services.AddSingleton<ICredentialHasher, Argon2CredentialHasher>();
        services.AddSingleton<IOtpVerifier, TotpOtpVerifier>();
        services.AddSingleton<IMfaSecretCipher, AesGcmMfaSecretCipher>();
        services.AddSingleton<IPinLookupDigester, HmacPinLookupDigester>();
        services.AddSingleton(Options.Create(new JwtOptions { Secret = TestJwtSecret }));
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        // US-035: RequestBillCommand/RequestBillByQrCommand/RegisterPartialPaymentCommand
        // consomem IAuthorizationTokenValidator diretamente (elevação pontual, ADR-023) para a
        // checagem BLOCK/WARN/IGNORE de item pendente — mesmo registro de Program.cs (Api.Edge/Cloud).
        services.AddSingleton<IAuthorizationTokenValidator, AuthorizationTokenValidator>();

        // E-08: AlertRaiser é dependência de MarkProductUnavailable/MarkProductAvailable/
        // RestoreProductsPastBusinessDay (US-080 §2 "produto indisponível") além dos comandos
        // próprios do motor de alertas — registrado aqui para qualquer teste que dispare esses
        // handlers pelo container real. IPushNotificationSender só é exercitado pelos testes do
        // próprio módulo de alertas (DeliverPendingPushCommand); LoggingPushNotificationSender
        // evita I/O de rede real durante o teste (mesmo espírito de LoggingEmailDispatcher).
        services.AddSingleton<IAlertRaiser, AlertRaiser>();
        services.AddSingleton<IPushNotificationSender, Nexora.Infrastructure.Notifications.LoggingPushNotificationSender>();

        // US-144 (Importação de cardápio por planilha): ValidateCatalogImportQueryHandler/
        // ImportCatalogCommandHandler/GetCatalogImportTemplateQueryHandler dependem de
        // ISpreadsheetParser — mesma implementação real de produção (ClosedXML), nenhum mock.
        services.AddSingleton<ISpreadsheetParser, ClosedXmlSpreadsheetParser>();

        // US-143 (Domínio próprio por cliente): registro real de ManualCertificateIssuer (dev-safe
        // default, nunca fala com uma CA de verdade — ver docstring da classe); verificação DNS por
        // padrão SEMPRE confirma (testes que precisam do cenário "Domínio não verificado" passam
        // um FakeDomainVerificationService(result: false) explícito); IPlatformAlertNotifier
        // reaproveita o mesmo LoggingPlatformAlertNotifier de produção (só log, nenhum I/O externo).
        services.AddSingleton(domainVerificationService ?? new FakeDomainVerificationService(result: true));
        services.AddSingleton<ICertificateIssuer>(certificateIssuer ?? new ManualCertificateIssuer());

        // US-152 (Visão 360 e acesso aos módulos do estabelecimento): GetTenantOverviewQueryHandler
        // depende de IPlatformLinksResolver — mesma implementação real de produção
        // (Infrastructure.Platform.PlatformLinksResolver), com um sufixo de domínio de teste para
        // que tenants sem domínio próprio ainda resolvam publicMenu/admin (nenhum mock de
        // infraestrutura, mesmo espírito dos demais registros deste factory).
        services.AddSingleton(Options.Create(new PlatformDomainOptions { DefaultDomainSuffix = "test.nexora.local" }));
        services.AddSingleton<IPlatformLinksResolver, Nexora.Infrastructure.Platform.PlatformLinksResolver>();
        if (platformAlertNotifier is not null)
        {
            services.AddSingleton(platformAlertNotifier);
        }
        else
        {
            services.AddSingleton<IPlatformAlertNotifier, Nexora.Infrastructure.Notifications.LoggingPlatformAlertNotifier>();
        }

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);

        return services.BuildServiceProvider();
    }
}
