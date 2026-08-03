using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installations.Abstractions;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Installations;
using Nexora.Infrastructure.Notifications;
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
        ITableMapBroadcaster? tableMapBroadcaster = null)
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
        services.AddSingleton(tableMapBroadcaster ?? new RecordingTableMapBroadcaster());
        // AddOrderItemCommand (gap de US-030, reaproveitado pelo cenário "Novo pedido após
        // solicitar a conta" da US-026) também depende de IOrderConsumptionBroadcaster.
        services.AddSingleton<IOrderConsumptionBroadcaster>(new RecordingOrderConsumptionBroadcaster());

        var authSecrets = Options.Create(new AuthSecretsOptions
        {
            SecretPepper = TestSecretPepper,
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
