using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;
using Nexora.Application.Cashier.Commands.CloseCashSession;
using Nexora.Application.Cashier.Commands.OpenCashSession;
using Nexora.Application.Cashier.Commands.RegisterCashMovement;
using Nexora.Application.Cashier.Queries.GetCurrentCashSession;
using Nexora.Application.Cashier.Queries.ListCashMovements;
using Nexora.Domain.Cashier;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Devices;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-055 (Abertura e fechamento de caixa) e US-056 (Sangria e suprimento)
/// contra um PostgreSQL real (Testcontainers), mesmo pipeline MediatR de produção (ADR-037).
/// Arquivo NOVO com container próprio (mesmo espírito de <c>PendingItemsOnCloseIntegrationTests</c>):
/// os cenários de mesa aberta autorizada e sangria acima do limite exigem a pilha REAL de
/// autorização pontual (ADR-023) com PIN real do gerente.
/// </summary>
[Collection("Postgres")]
public sealed class CashSessionIntegrationTests
{
    private const string TestJwtSecret = "cash-session-integration-test-jwt-secret-32-bytes!!";
    private const string TestPinLookupPepper = "cash-session-integration-test-pin-lookup-pepper-32b!";

    private readonly PostgresFixture _fixture;

    public CashSessionIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Abertura com fundo" (US-055 §4).</summary>
    [Fact]
    public async Task Abrir_Caixa_Com_Fundo_Cria_Sessao_Open_E_Emite_Evento()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new OpenCashSessionCommand(200m));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Status.Should().Be("OPEN");
        result.Value.OpeningAmount.Should().Be(200m);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var evt = await verifyDb.DomainEvents.SingleAsync(e => e.AggregateId == result.Value.Id && e.Type == "cash.session.opened");
        evt.AggregateType.Should().Be("cash_session");
    }

    /// <summary>Cenário Gherkin "Um caixa por operador e turno" (US-055 §4).</summary>
    [Fact]
    public async Task Abrir_Segundo_Caixa_Para_O_Mesmo_Operador_Recebe_409_Apontando_A_Sessao_Existente()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(new OpenCashSessionCommand(100m));
        first.IsSuccess.Should().BeTrue();

        await using var secondDb = _fixture.CreateAppDbContext(tenantContext);
        await using var secondProvider = BuildContainer(secondDb, tenantContext);
        var second = await secondProvider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(50m));

        second.IsFailure.Should().BeTrue();
        second.Code.Should().Be(ApiErrorCodes.CashSessionAlreadyOpen);
        second.Errors!["sessionId"].Single().Should().Be(first.Value!.Id.ToString());
    }

    /// <summary>Cenário Gherkin "Composição do valor esperado" (US-055 §4): fundo 200 + 1.500 em dinheiro + 300 de suprimento − 150 de sangria = 1.850.</summary>
    [Fact]
    public async Task Composicao_Do_Valor_Esperado_Soma_Abertura_Pagamentos_Suprimentos_E_Sangrias()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(200m));
            sessionId = opened.Value!.Id;
        }

        await SeedCashPaymentAsync(world, sessionId, amount: 1500m, changeAmount: 0m);

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var sender = provider.GetRequiredService<ISender>();
            var supply = await sender.Send(new RegisterCashMovementCommand("SUPPLY", 300m, "Reforço de troco", null));
            supply.IsSuccess.Should().BeTrue(supply.IsFailure ? supply.Code : string.Empty);
        }

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var sender = provider.GetRequiredService<ISender>();
            var withdrawal = await sender.Send(new RegisterCashMovementCommand("WITHDRAWAL", 150m, "Sangria de segurança", null));
            withdrawal.IsSuccess.Should().BeTrue(withdrawal.IsFailure ? withdrawal.Code : string.Empty);
            withdrawal.Value!.NewExpected.Should().Be(1850m);
        }

        await using var finalDb = _fixture.CreateAppDbContext(tenantContext);
        await using var finalProvider = BuildContainer(finalDb, tenantContext);
        var current = await finalProvider.GetRequiredService<ISender>().Send(new GetCurrentCashSessionQuery());

        current.IsSuccess.Should().BeTrue();
        current.Value!.Expected.Opening.Should().Be(200m);
        current.Value.Expected.CashPayments.Should().Be(1500m);
        current.Value.Expected.Supplies.Should().Be(300m);
        current.Value.Expected.Withdrawals.Should().Be(-150m);
        current.Value.Expected.Total.Should().Be(1850m);
    }

    /// <summary>Cenário Gherkin "Fechamento sem divergência" (US-055 §4).</summary>
    [Fact]
    public async Task Fechamento_Sem_Divergencia_Vai_Para_Closed_Sem_Exigir_Justificativa()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(200m));
            sessionId = opened.Value!.Id;
        }

        await using var closeDb = _fixture.CreateAppDbContext(tenantContext);
        await using var closeProvider = BuildContainer(closeDb, tenantContext);
        var result = await closeProvider.GetRequiredService<ISender>().Send(
            new CloseCashSessionCommand(sessionId, 200m, null, null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.RequiresJustification.Should().BeFalse();
        result.Value.Divergence.Should().Be(0m);
        result.Value.Session.Status.Should().Be("CLOSED");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.CashSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(CashSessionStatus.Closed);
        session.Justification.Should().BeNull();
    }

    /// <summary>Cenário Gherkin "Divergência no fechamento" (US-055 §4): sem justificativa é recusado; com justificativa fecha e alerta o gestor.</summary>
    [Fact]
    public async Task Divergencia_Acima_Do_Limiar_Exige_Justificativa_E_Alerta_O_Gestor()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext, alertsBroadcaster))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(200m));
            sessionId = opened.Value!.Id;
        }

        // Sem justificativa: divergência de R$ 10 (acima do default de R$ 5) é recusada.
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext, alertsBroadcaster))
        {
            var refused = await provider.GetRequiredService<ISender>().Send(
                new CloseCashSessionCommand(sessionId, 190m, null, null));
            refused.IsFailure.Should().BeTrue();
            refused.Code.Should().Be(ApiErrorCodes.CashJustificationRequired);
        }

        await using var closeDb = _fixture.CreateAppDbContext(tenantContext);
        await using var closeProvider = BuildContainer(closeDb, tenantContext, alertsBroadcaster);
        var result = await closeProvider.GetRequiredService<ISender>().Send(
            new CloseCashSessionCommand(sessionId, 190m, "Troco entregue a mais em um pedido", null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.RequiresJustification.Should().BeTrue();
        result.Value.Divergence.Should().Be(-10m);
        result.Value.Session.Status.Should().Be("CLOSED");

        alertsBroadcaster.AlertRaisedCalls.Should().Contain(c => c.Type == AlertTypes.CashDivergence, "US-055 §4: o gestor deve ser alertado");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.CashSessions.SingleAsync(s => s.Id == sessionId);
        session.Justification.Should().Be("Troco entregue a mais em um pedido");
    }

    /// <summary>Cenário Gherkin "Mesa aberta no fechamento" (US-055 §4/RN-018) — bloqueio.</summary>
    [Fact]
    public async Task Mesa_Aberta_Bloqueia_O_Fechamento_E_Lista_As_Mesas_Abertas()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        var tableLabel = await OpenTableSessionAsync(world);

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(200m));
            sessionId = opened.Value!.Id;
        }

        await using var closeDb = _fixture.CreateAppDbContext(tenantContext);
        await using var closeProvider = BuildContainer(closeDb, tenantContext);
        var result = await closeProvider.GetRequiredService<ISender>().Send(
            new CloseCashSessionCommand(sessionId, 200m, null, null));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.OpenTables);
        result.Errors.Should().ContainKey(Application.Cashier.Support.CashCloseGuard.MetaErrorsKey);
        result.Errors![Application.Cashier.Support.CashCloseGuard.MetaErrorsKey].Single().Should().Contain(tableLabel);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.CashSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(CashSessionStatus.Open, "a recusa não muda o estado do caixa");
    }

    /// <summary>Contorno do bloqueio acima com autorização de perfil superior (RN-018).</summary>
    [Fact]
    public async Task Mesa_Aberta_Autorizada_Prossegue_E_Audita_Autor_E_Autorizador()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        await OpenTableSessionAsync(world);
        const string managerPin = "9911";
        var managerId = await SeedManagerWithPermissionAsync(world, managerPin, "cash:close_divergent");

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(200m));
            sessionId = opened.Value!.Id;
        }

        await using var authDb = _fixture.CreateAppDbContext(tenantContext);
        await using var authProvider = BuildContainer(authDb, tenantContext);
        var authorized = await authProvider.GetRequiredService<ISender>().Send(new AuthorizeSensitiveActionCommand(
            "CLOSE_DIVERGENT_CASH", managerPin, new Dictionary<string, object?> { ["cashSessionId"] = sessionId.ToString() }));
        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);

        await using var closeDb = _fixture.CreateAppDbContext(tenantContext);
        await using var closeProvider = BuildContainer(closeDb, tenantContext);
        var result = await closeProvider.GetRequiredService<ISender>().Send(
            new CloseCashSessionCommand(sessionId, 200m, null, authorized.Value!.AuthorizationToken));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Session.Status.Should().Be("CLOSED");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "CLOSE_CASH_WITH_OPEN_TABLES" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(world.OperatorId, "executor — o operador que estava fechando o caixa");
        audit.AuthorizedBy.Should().Be(managerId, "autorizador — o gerente que informou o PIN");
    }

    /// <summary>
    /// Cenário Gherkin "Sangria registrada" (US-056 §4) — os R$ 500,00 do próprio Gherkin excedem o
    /// default de <c>maxWithdrawalWithoutAuth</c> (R$ 300,00, cenário irmão "Sangria acima do
    /// limite"); os dois cenários não compartilham o mesmo "Dado", então este configura um limite
    /// mais alto para isolar exatamente o que a história pede aqui — o efeito no valor esperado.
    /// </summary>
    [Fact]
    public async Task Sangria_Registrada_Reduz_O_Valor_Esperado_E_Emite_Evento()
    {
        var world = await SeedWorldAsync();
        await SeedMaxWithdrawalWithoutAuthAsync(world.TenantId, 1000.00m);
        var tenantContext = OperatorContext(world);

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(1500m));
            sessionId = opened.Value!.Id;
        }

        await using var db2 = _fixture.CreateAppDbContext(tenantContext);
        await using var provider2 = BuildContainer(db2, tenantContext);
        var result = await provider2.GetRequiredService<ISender>().Send(
            new RegisterCashMovementCommand("WITHDRAWAL", 500m, "Sangria de segurança", null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.NewExpected.Should().Be(1000m);
        result.Value.Movement.Type.Should().Be("WITHDRAWAL");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var evt = await verifyDb.DomainEvents.SingleAsync(e => e.Type == "cash.movement.registered" && e.AggregateId == result.Value.Movement.Id);
        evt.AggregateType.Should().Be("cash_movement");
    }

    /// <summary>Cenário Gherkin "Suprimento de troco" (US-056 §4).</summary>
    [Fact]
    public async Task Suprimento_Registrado_Aumenta_O_Valor_Esperado()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);

        Guid sessionId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(0m));
            sessionId = opened.Value!.Id;
        }

        await using var db2 = _fixture.CreateAppDbContext(tenantContext);
        await using var provider2 = BuildContainer(db2, tenantContext);
        var result = await provider2.GetRequiredService<ISender>().Send(
            new RegisterCashMovementCommand("SUPPLY", 200m, "Troco inicial", null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.NewExpected.Should().Be(200m);
    }

    /// <summary>Cenário Gherkin "Sangria acima do limite" (US-056 §4): "Dado o limite de sangria sem autorização em R$ 300,00".</summary>
    [Fact]
    public async Task Sangria_Acima_Do_Limite_Exige_Autorizacao_De_Perfil_Superior()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        const string managerPin = "4433";
        var managerId = await SeedManagerWithPermissionAsync(world, managerPin, "cash:withdraw_any");

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var opened = await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(1500m));
        }

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            var refused = await provider.GetRequiredService<ISender>().Send(
                new RegisterCashMovementCommand("WITHDRAWAL", 800m, "Depósito no banco", null));
            refused.IsFailure.Should().BeTrue();
            refused.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
        }

        await using var authDb = _fixture.CreateAppDbContext(tenantContext);
        await using var authProvider = BuildContainer(authDb, tenantContext);
        var authorized = await authProvider.GetRequiredService<ISender>().Send(new AuthorizeSensitiveActionCommand(
            "WITHDRAWAL_ABOVE_LIMIT", managerPin, new Dictionary<string, object?> { ["amount"] = "800.00" }));
        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);

        await using var retryDb = _fixture.CreateAppDbContext(tenantContext);
        await using var retryProvider = BuildContainer(retryDb, tenantContext);
        var result = await retryProvider.GetRequiredService<ISender>().Send(
            new RegisterCashMovementCommand("WITHDRAWAL", 800m, "Depósito no banco", authorized.Value!.AuthorizationToken));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Movement.AuthorizedBy.Should().Be(managerId);
    }

    /// <summary>Cenário Gherkin "Movimento sem caixa aberto" (US-056 §4).</summary>
    [Fact]
    public async Task Movimento_Sem_Caixa_Aberto_Recebe_409()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(
            new RegisterCashMovementCommand("SUPPLY", 50m, "Troco", null));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.NoOpenCashSession);
    }

    /// <summary>US-056 §7/§10: histórico do turno acessível na mesma tela.</summary>
    [Fact]
    public async Task Historico_Do_Turno_Lista_Movimentos_Mais_Recentes_Primeiro()
    {
        var world = await SeedWorldAsync();
        var tenantContext = OperatorContext(world);

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            await provider.GetRequiredService<ISender>().Send(new OpenCashSessionCommand(500m));
        }

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            await provider.GetRequiredService<ISender>().Send(new RegisterCashMovementCommand("SUPPLY", 100m, "Troco", null));
        }

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db, tenantContext))
        {
            await provider.GetRequiredService<ISender>().Send(new RegisterCashMovementCommand("WITHDRAWAL", 50m, "Sangria", null));
        }

        await using var listDb = _fixture.CreateAppDbContext(tenantContext);
        await using var listProvider = BuildContainer(listDb, tenantContext);
        var result = await listProvider.GetRequiredService<ISender>().Send(new ListCashMovementsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Movements.Should().HaveCount(2);
        result.Value.Movements.Select(m => m.Type).Should().Contain(new[] { "SUPPLY", "WITHDRAWAL" });
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId, Guid OperatorId, Guid DeviceId);

    private async Task<World> SeedWorldAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        storeDb.Areas.Add(Area.Create(tenantId, storeId, "Salão de teste"));
        var device = Device.Create(tenantId, storeId, "Terminal de teste", DeviceType.Waiter, $"fingerprint-{Guid.NewGuid():N}");
        storeDb.Devices.Add(device);

        // AppUser.Create exige senha OU PIN definido — o operador destes testes nunca autentica de
        // verdade (o teste chama os handlers via ISender direto, sem passar pelo login), então um
        // hash fixo qualquer basta para satisfazer o invariante do domínio.
        var operatorUser = AppUser.Create(
            tenantId, "Caixa de teste", email: null, passwordHash: null, pinHash: "unused-pin-hash", pinLookup: null);
        storeDb.Users.Add(operatorUser);

        await storeDb.SaveChangesAsync();

        var areaId = (await storeDb.Areas.SingleAsync()).Id;
        return new World(tenantId, storeId, areaId, operatorUser.Id, device.Id);
    }

    private static StaticTenantContext OperatorContext(World world) =>
        new(world.TenantId, world.StoreId, userId: world.OperatorId, deviceId: world.DeviceId);

    private async Task SeedCashPaymentAsync(World world, Guid cashSessionId, decimal amount, decimal changeAmount)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var payment = Payment.Create(
            world.TenantId, world.StoreId, DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod.Cash,
            amount: amount, netAmount: amount, changeAmount: changeAmount, cashSessionId: cashSessionId, createdBy: world.OperatorId);
        payment.MarkPaid(DateTimeOffset.UtcNow);
        db.Payments.Add(payment);

        await db.SaveChangesAsync();
    }

    /// <summary>US-056 §8 — configura <c>operation.maxWithdrawalWithoutAuth</c> para um cenário específico (default é R$ 300,00, ver <c>CashPolicy</c>).</summary>
    private async Task SeedMaxWithdrawalWithoutAuthAsync(Guid tenantId, decimal limit)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var config = await db.TenantConfigs.SingleOrDefaultAsync(c => c.TenantId == tenantId);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            db.TenantConfigs.Add(config);
        }

        config.UpdateOperation($$"""{"maxWithdrawalWithoutAuth": {{limit.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}""");
        await db.SaveChangesAsync();
    }

    /// <summary>Abre uma sessão de mesa (RN-018 "mesa aberta") e devolve o rótulo — usado pelos cenários de bloqueio de fechamento.</summary>
    private async Task<string> OpenTableSessionAsync(World world)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"T-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"qr-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return table.Label;
    }

    /// <summary>Gerente com PIN real (Argon2) e a permissão elevável — mesmo espírito de <c>PendingItemsOnCloseIntegrationTests.SeedManagerWithPermissionAsync</c>.</summary>
    private async Task<Guid> SeedManagerWithPermissionAsync(World world, string pin, string permission)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var hasher = new Argon2CredentialHasher();
        var pinDigester = new HmacPinLookupDigester(Options.Create(new AuthSecretsOptions { PinLookupPepper = TestPinLookupPepper }));

        var role = Role.Create(world.TenantId, "MANAGER", "Gerente de teste", isSystem: true);
        role.UpdatePermissions($"[\"{permission}\"]");
        db.Roles.Add(role);

        var manager = AppUser.Create(
            world.TenantId, "Gerente de teste", email: null, passwordHash: null, pinHash: hasher.Hash(pin), pinLookup: pinDigester.Digest(pin));
        db.Users.Add(manager);
        db.UserRoles.Add(UserRole.Create(world.TenantId, manager.Id, role.Id));

        await db.SaveChangesAsync();

        return manager.Id;
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext, IAlertsBroadcaster? alertsBroadcaster = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton(alertsBroadcaster ?? new RecordingAlertsBroadcaster());
        services.AddScoped<IAlertRaiser, AlertRaiser>();

        // Auth (ADR-023) — pilha REAL para exercitar AuthorizeSensitiveActionCommand -> CloseCashSessionCommand/
        // RegisterCashMovementCommand de ponta a ponta (mesa aberta autorizada, sangria acima do limite).
        services.AddSingleton<ICredentialHasher, Argon2CredentialHasher>();
        services.AddSingleton<IPinLookupDigester>(new HmacPinLookupDigester(Options.Create(new AuthSecretsOptions { PinLookupPepper = TestPinLookupPepper })));
        services.AddSingleton(Options.Create(new JwtOptions { Secret = TestJwtSecret }));
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IAuthorizationTokenValidator, Nexora.Application.Auth.Shared.AuthorizationTokenValidator>();

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
