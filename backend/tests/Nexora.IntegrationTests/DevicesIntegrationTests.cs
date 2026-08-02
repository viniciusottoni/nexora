using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices.Abstractions;
using Nexora.Application.Devices.Commands.CreatePairingCode;
using Nexora.Application.Devices.Commands.PairDevice;
using Nexora.Application.Devices.Commands.RevokeDevice;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-005 contra um PostgreSQL real (Testcontainers, mesma <see cref="PostgresFixture"/>
/// da US-001) e o mesmo pipeline MediatR de produção (Validation -&gt; Logging -&gt; Transaction) —
/// "Pareamento de novo terminal", "Revogação de dispositivo" e a exigência de segurança do §12
/// ("rate limit + expiração curta"). Não cobre pareamento/revogação via HTTP (isso é
/// <c>Nexora.ApiTests</c>/E2E); aqui o alvo é a regra de negócio persistida sob RLS real.
/// </summary>
[Collection("Postgres")]
public sealed class DevicesIntegrationTests
{
    private const string Pepper = "test-pepper-nao-e-segredo-de-producao";

    private readonly PostgresFixture _fixture;

    public DevicesIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Pareamento de novo terminal" — código de uso único, dispositivo registrado, audit_log e evento gravados.</summary>
    [Fact]
    public async Task Pareamento_Completo_Registra_Dispositivo_Consome_Codigo_E_Grava_Auditoria_E_Evento()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var codeResult = await sender.Send(new CreatePairingCodeCommand());
        codeResult.IsSuccess.Should().BeTrue();
        var code = codeResult.Value!.Code;

        var pairResult = await sender.Send(new PairDeviceCommand(code, "Caixa 1", "CASHIER", "fingerprint-abc"));

        pairResult.IsSuccess.Should().BeTrue();
        pairResult.Value!.Device.Label.Should().Be("Caixa 1");
        pairResult.Value!.DeviceSecret.Should().NotBeNullOrWhiteSpace();

        var device = await db.Devices.SingleAsync(d => d.Id == pairResult.Value!.Device.Id);
        device.IsActive.Should().BeTrue();
        device.TenantId.Should().Be(tenantId);
        device.StoreId.Should().Be(storeId);

        // "o código deve ser invalidado" — reusar o MESMO código depois é recusado.
        var pairingCode = await db.PairingCodes.SingleAsync(p => p.TenantId == tenantId && p.StoreId == storeId);
        pairingCode.IsConsumed.Should().BeTrue();

        var reuseResult = await sender.Send(new PairDeviceCommand(code, "Caixa 2", "CASHIER", "fingerprint-xyz"));
        reuseResult.IsSuccess.Should().BeFalse();
        reuseResult.Code.Should().Be(ApiErrorCodes.DevicePairingCodeConsumed);

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "DEVICE_REGISTERED");
        auditEntry.EntityId.Should().Be(device.Id);

        var domainEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "device.registered");
        domainEvent.AggregateId.Should().Be(device.Id);
    }

    /// <summary>Cenário Gherkin "Código expirado" — recusado com o código de erro certo, sem consumir nem registrar dispositivo.</summary>
    [Fact]
    public async Task Pareamento_Com_Codigo_Expirado_E_Recusado_Sem_Registrar_Dispositivo()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();
        const string rawCode = "418302";

        await using var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        var digester = new DeviceSecretDigester(Options.Create(new DeviceSecurityOptions { Pepper = Pepper }));
        seedDb.PairingCodes.Add(PairingCode.Create(
            tenantId, storeId, digester.Digest(rawCode), managerId, DateTimeOffset.UtcNow.AddMinutes(-1)));
        await seedDb.SaveChangesAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new PairDeviceCommand(rawCode, "Caixa 1", "CASHIER", "fingerprint-abc"));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.DevicePairingCodeExpired);
        (await db.Devices.AnyAsync(d => d.TenantId == tenantId)).Should().BeFalse();
    }

    /// <summary>
    /// Cenário Gherkin "Revogação de dispositivo" — encerra toda sessão ativa do dispositivo
    /// imediatamente e grava audit_log, na mesma transação (ADR-006).
    /// </summary>
    [Fact]
    public async Task Revogacao_Encerra_Sessoes_Ativas_E_Grava_Audit_Log()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        Guid operatorUserId;
        Guid deviceId;
        Guid activeSessionId;
        Guid alreadyRevokedSessionId;

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId)))
        {
            // auth_session.user_id tem FK para app_user (diferente de audit_log.actor_id, que não
            // é FK-constrained) — precisa existir de fato para o SaveChangesAsync não falhar.
            var operatorUser = AppUser.Create(tenantId, "Garçom de teste", email: null, passwordHash: null, pinHash: "hash-pin-irrelevante");
            seedDb.Users.Add(operatorUser);
            operatorUserId = operatorUser.Id;

            var seedDevice = Device.Create(tenantId, storeId, "Garçom 1", DeviceType.Waiter, "fingerprint-garcom-1");
            seedDevice.SetSecret("hash-irrelevante-para-o-teste");
            seedDb.Devices.Add(seedDevice);
            deviceId = seedDevice.Id;

            var seedActiveSession = AuthSession.Create(tenantId, operatorUserId, deviceId, refreshHash: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
            var seedAlreadyRevokedSession = AuthSession.Create(tenantId, operatorUserId, deviceId, refreshHash: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
            seedAlreadyRevokedSession.Revoke();

            seedDb.AuthSessions.Add(seedActiveSession);
            seedDb.AuthSessions.Add(seedAlreadyRevokedSession);
            activeSessionId = seedActiveSession.Id;
            alreadyRevokedSessionId = seedAlreadyRevokedSession.Id;

            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RevokeDeviceCommand(deviceId));

        result.IsSuccess.Should().BeTrue();

        var device = await db.Devices.SingleAsync(d => d.Id == deviceId);
        device.IsActive.Should().BeFalse("a revogação é uma mudança de status — o registro histórico permanece para auditoria, nunca é excluído fisicamente");

        var activeSession = await db.AuthSessions.SingleAsync(s => s.Id == activeSessionId);
        activeSession.RevokedAt.Should().NotBeNull("toda sessão ativa do dispositivo revogado deve ser encerrada imediatamente");

        // Sessão que já estava revogada antes não deveria ser tocada de novo (Revoke() lança se já revogada).
        var alreadyRevokedSession = await db.AuthSessions.SingleAsync(s => s.Id == alreadyRevokedSessionId);
        alreadyRevokedSession.RevokedAt.Should().NotBeNull();

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "DEVICE_REVOKED");
        auditEntry.EntityId.Should().Be(deviceId);
        auditEntry.ActorId.Should().Be(managerId);
    }

    /// <summary>
    /// Cenário de segurança do §12 ("Código de pareamento não é adivinhável por força bruta —
    /// rate limit + expiração curta"): 5 tentativas com código errado esgotam o limite da janela
    /// de 15 min; a 6ª tentativa é recusada por rate limit, mesmo com o código certo. Cada
    /// tentativa usa uma instância de <see cref="IApplicationDbContext"/> própria — como uma
    /// requisição HTTP real usaria (DbContext Scoped por requisição) — para provar que o contador
    /// de tentativas persiste no banco entre requisições, não só na memória de um processo.
    /// </summary>
    [Fact]
    public async Task Pareamento_Apos_Cinco_Tentativas_Invalidas_Ativa_Rate_Limit()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        string correctCode;
        await using (var setupDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId)))
        await using (var setupProvider = BuildMediatRContainer(setupDb, new StaticTenantContext(tenantId, storeId, managerId)))
        {
            var codeResult = await setupProvider.GetRequiredService<ISender>().Send(new CreatePairingCodeCommand());
            codeResult.IsSuccess.Should().BeTrue();
            correctCode = codeResult.Value!.Code;
        }

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await using var attemptDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
            await using var attemptProvider = BuildMediatRContainer(attemptDb, new StaticTenantContext(tenantId, storeId, managerId));

            var wrongAttempt = await attemptProvider.GetRequiredService<ISender>()
                .Send(new PairDeviceCommand("000000", $"Tentativa {attempt}", "CASHIER", $"fingerprint-{attempt}"));

            wrongAttempt.IsSuccess.Should().BeFalse();
            wrongAttempt.Code.Should().Be(ApiErrorCodes.DevicePairingCodeInvalid);
        }

        await using var finalDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var finalProvider = BuildMediatRContainer(finalDb, new StaticTenantContext(tenantId, storeId, managerId));

        var rateLimitedResult = await finalProvider.GetRequiredService<ISender>()
            .Send(new PairDeviceCommand(correctCode, "Caixa 1", "CASHIER", "fingerprint-final"));

        rateLimitedResult.IsSuccess.Should().BeFalse();
        rateLimitedResult.Code.Should().Be(ApiErrorCodes.DevicePairingRateLimited, "5 tentativas erradas dentro da janela de 15 min devem acionar o rate limit mesmo antes de validar o código da 6ª tentativa");
        (await finalDb.Devices.AnyAsync(d => d.TenantId == tenantId)).Should().BeFalse("nenhuma das tentativas — nem a última, bloqueada pelo rate limit — deveria ter registrado um dispositivo");
    }

    private async Task<(Guid TenantId, Guid StoreId)> SeedTenantAndStoreAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        await storeDb.SaveChangesAsync();

        return (tenantId, storeId);
    }

    private static ServiceProvider BuildMediatRContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);

        services.AddSingleton<IPairingCodeGenerator, PairingCodeGenerator>();
        services.AddSingleton<IDeviceSecretGenerator, DeviceSecretGenerator>();
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton<ISecretDigester>(
            new DeviceSecretDigester(Options.Create(new DeviceSecurityOptions { Pepper = Pepper })));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }
}
