using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Tables.Commands.AcknowledgeWaiterCall;
using Nexora.Application.Tables.Commands.CallWaiter;
using Nexora.Application.Tables.Commands.RequestBill;
using Nexora.Application.Tables.Commands.RequestBillByQr;
using Nexora.Application.Tables.Queries.GetTableMap;
using Nexora.Application.Tables.Queries.GetTableSession;
using Nexora.Contracts.Tables;
using Nexora.Domain.Catalog;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-025 (Chamar garçom pela mesa) e US-026 (Solicitar a conta) contra um
/// PostgreSQL real (Testcontainers) — mesmo pipeline MediatR de produção
/// (<see cref="MediatRTestContainerFactory"/>).
/// </summary>
[Collection("Postgres")]
public sealed class WaiterCallAndBillRequestIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public WaiterCallAndBillRequestIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Chamada direcionada" (US-025 §4): só o garçom responsável é notificado.</summary>
    [Fact]
    public async Task Chamada_Direcionada_Alerta_So_O_Garcom_Responsavel()
    {
        var world = await SeedWorldAsync();
        var ana = Guid.NewGuid();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "12", waiterId: ana);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();
        var tableMapBroadcaster = new RecordingTableMapBroadcaster();
        await using var provider = MediatRTestContainerFactory.Build(
            db, tenantContext, alertsBroadcaster, tableMapBroadcaster: tableMapBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CallWaiterCommand(qrToken, sessionId, tableId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Acknowledged.Should().BeTrue();
        result.Value.AlreadyPending.Should().BeFalse();

        alertsBroadcaster.WaiterCalledCalls.Should().ContainSingle(c => c.WaiterId == ana && c.TableId == tableId);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alert = await verifyDb.Alerts.SingleAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.WaiterCalled);
        alert.TargetUserId.Should().Be(ana, "o alerta é dirigido ao garçom responsável, não a todos");
        alert.TargetRoles.Should().BeEmpty("demais garçons não devem ser notificados quando já há responsável");
        alert.ResolvedAt.Should().BeNull();

        var map = await sender.Send(new GetTableMapQuery(MineOnly: false, TableMapSortBy.Label));
        map.IsSuccess.Should().BeTrue();
        map.Value!.Tables.Single(t => t.Id == tableId).Flags.WaiterCalled.Should().BeTrue();
    }

    /// <summary>US-025 §3.1: sessão sem garçom responsável ainda (aberta por QR) escala para o papel inteiro.</summary>
    [Fact]
    public async Task Chamada_Sem_Garcom_Responsavel_Usa_Role_Waiter_Como_Fallback()
    {
        var world = await SeedWorldAsync();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "13", waiterId: null);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext, alertsBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CallWaiterCommand(qrToken, sessionId, tableId));

        result.IsSuccess.Should().BeTrue();
        alertsBroadcaster.WaiterCalledCalls.Should().ContainSingle(c => c.WaiterId == null);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alert = await verifyDb.Alerts.SingleAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.WaiterCalled);
        alert.TargetRoles.Should().Contain("WAITER");
        alert.TargetUserId.Should().BeNull();
    }

    /// <summary>Cenário Gherkin "Chamada repetida" (US-025 §4): não cria um segundo alerta.</summary>
    [Fact]
    public async Task Chamada_Repetida_Nao_Cria_Novo_Alerta()
    {
        var world = await SeedWorldAsync();
        var ana = Guid.NewGuid();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "14", waiterId: ana);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext, alertsBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(new CallWaiterCommand(qrToken, sessionId, tableId));
        var second = await sender.Send(new CallWaiterCommand(qrToken, sessionId, tableId));

        first.Value!.AlreadyPending.Should().BeFalse();
        second.IsSuccess.Should().BeTrue();
        second.Value!.AlreadyPending.Should().BeTrue("o garçom já foi avisado — não deve ser criado um novo alerta");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alertCount = await verifyDb.Alerts.CountAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.WaiterCalled);
        alertCount.Should().Be(1);
        alertsBroadcaster.WaiterCalledCalls.Should().HaveCount(1, "a segunda chamada não deve disparar um segundo broadcast");
    }

    /// <summary>Cenário Gherkin "Confirmação de atendimento" (US-025 §4): o indicador some do mapa e o alerta é resolvido.</summary>
    [Fact]
    public async Task Confirmar_Atendimento_Resolve_O_Alerta_E_Some_Do_Mapa()
    {
        var world = await SeedWorldAsync();
        var ana = Guid.NewGuid();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "15", waiterId: ana);

        var clientContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var clientDb = _fixture.CreateAppDbContext(clientContext);
        await using var clientProvider = MediatRTestContainerFactory.Build(clientDb, clientContext);
        await clientProvider.GetRequiredService<ISender>().Send(new CallWaiterCommand(qrToken, sessionId, tableId));

        var waiterContext = new StaticTenantContext(world.TenantId, world.StoreId, ana);
        await using var waiterDb = _fixture.CreateAppDbContext(waiterContext);
        var tableMapBroadcaster = new RecordingTableMapBroadcaster();
        await using var waiterProvider = MediatRTestContainerFactory.Build(waiterDb, waiterContext, tableMapBroadcaster: tableMapBroadcaster);
        var sender = waiterProvider.GetRequiredService<ISender>();

        var acknowledged = await sender.Send(new AcknowledgeWaiterCallCommand(tableId));

        acknowledged.IsSuccess.Should().BeTrue();
        acknowledged.Value!.Resolved.Should().BeTrue();
        acknowledged.Value.ResponseSeconds.Should().BeGreaterThanOrEqualTo(0);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alert = await verifyDb.Alerts.SingleAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.WaiterCalled);
        alert.ResolvedAt.Should().NotBeNull();
        alert.AcknowledgedBy.Should().Be(ana);

        var map = await sender.Send(new GetTableMapQuery(MineOnly: false, TableMapSortBy.Label));
        map.Value!.Tables.Single(t => t.Id == tableId).Flags.WaiterCalled.Should().BeFalse("o indicador deve desaparecer do mapa");
    }

    /// <summary>Segurança (RN-015): confirmar atendimento sem nenhuma chamada pendente nunca quebra — devolve um código estável, nunca 403.</summary>
    [Fact]
    public async Task Confirmar_Atendimento_Sem_Chamada_Pendente_Devolve_Codigo_Estavel()
    {
        var world = await SeedWorldAsync();
        var (tableId, _, _) = await OpenSessionAsync(world, "16", waiterId: null);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new AcknowledgeWaiterCallCommand(tableId));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.NoPendingWaiterCall);
    }

    /// <summary>Cenário Gherkin "Solicitação pelo cliente" (US-026 §4): transição, alerta ao caixa/garçom e preferência registrada.</summary>
    [Fact]
    public async Task Solicitar_Conta_Pelo_Cliente_Transiciona_E_Alerta_Caixa_E_Garcom()
    {
        var world = await SeedWorldAsync();
        var ana = Guid.NewGuid();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "17", waiterId: ana);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext, alertsBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RequestBillByQrCommand(qrToken, sessionId, tableId, "BY_PERSON", 4));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Session.Status.Should().Be("BILLREQUESTED");
        result.Value.AlreadyRequested.Should().BeFalse();

        alertsBroadcaster.BillRequestedCalls.Should().ContainSingle(
            c => c.TableId == tableId && c.SplitMode == "BY_PERSON" && c.People == 4 && c.WaiterId == ana);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.BillRequested);
        session.SplitMode.Should().Be("BY_PERSON");
        session.SplitPeople.Should().Be(4);

        var alert = await verifyDb.Alerts.SingleAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.BillRequested);
        alert.TargetRoles.Should().Contain("CASHIER");
        alert.TargetUserId.Should().Be(ana);

        // "Preferência de divisão chega ao caixa" — recuperável via consulta simples da sessão.
        var fetched = await sender.Send(new GetTableSessionQuery(sessionId));
        fetched.IsSuccess.Should().BeTrue();
        fetched.Value!.SplitMode.Should().Be("BY_PERSON");
        fetched.Value.SplitPeople.Should().Be(4);
    }

    /// <summary>Cenário Gherkin "Solicitação pelo garçom" (US-026 §4): efeito idêntico ao da solicitação pelo cliente.</summary>
    [Fact]
    public async Task Solicitar_Conta_Pelo_Garcom_Tem_Efeito_Identico()
    {
        var world = await SeedWorldAsync();
        var (_, sessionId, _) = await OpenSessionAsync(world, "18", waiterId: null);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RequestBillCommand(sessionId, "SINGLE", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Session.Status.Should().Be("BILLREQUESTED");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.BillRequested);
        session.SplitMode.Should().Be("SINGLE");
    }

    /// <summary>Pedir a conta duas vezes é idempotente: atualiza a preferência sem criar um segundo alerta ao caixa.</summary>
    [Fact]
    public async Task Solicitar_Conta_Novamente_Atualiza_Preferencia_Sem_Duplicar_Alerta()
    {
        var world = await SeedWorldAsync();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "19", waiterId: null);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext, alertsBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new RequestBillByQrCommand(qrToken, sessionId, tableId, "BY_PERSON", 3));
        var second = await sender.Send(new RequestBillByQrCommand(qrToken, sessionId, tableId, "BY_PERSON", 5));

        second.IsSuccess.Should().BeTrue();
        second.Value!.AlreadyRequested.Should().BeTrue();
        second.Value.Session.SplitPeople.Should().Be(5, "a preferência é atualizada mesmo numa re-solicitação");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alertCount = await verifyDb.Alerts.CountAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.BillRequested);
        alertCount.Should().Be(1, "só a primeira solicitação cria alerta ao caixa");
        alertsBroadcaster.BillRequestedCalls.Should().HaveCount(1);
    }

    /// <summary>Cenário Gherkin "Novo pedido após solicitar a conta" (US-026 §4): a sessão volta para OPEN e o caixa é avisado.</summary>
    [Fact]
    public async Task Novo_Pedido_Apos_Solicitar_Conta_Volta_A_Sessao_Para_Open_E_Avisa_O_Caixa()
    {
        var world = await SeedWorldAsync();
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Marguerita", "Média", unitPrice: 42m);
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "20", waiterId: null);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var alertsBroadcaster = new RecordingAlertsBroadcaster();
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext, alertsBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var billRequested = await sender.Send(new RequestBillByQrCommand(qrToken, sessionId, tableId, "SINGLE", null));
        billRequested.IsSuccess.Should().BeTrue();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue("adicionar item numa comanda com conta solicitada é aceito, não bloqueado");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.Open, "US-026: novo pedido devolve a sessão para OPEN");
        session.SplitMode.Should().BeNull("a preferência da solicitação anterior foi invalidada");

        alertsBroadcaster.BillRequestCancelledCalls.Should().ContainSingle(c => c.TableId == tableId);

        var evt = await verifyDb.DomainEvents.SingleOrDefaultAsync(e => e.AggregateId == sessionId && e.Type == "table.session.reopened");
        evt.Should().NotBeNull();
    }

    /// <summary>Segurança (RN-015): pedido de conta com sessão de OUTRA mesa nunca é aceito — 404 genérico, nunca revela a outra mesa.</summary>
    [Fact]
    public async Task Solicitar_Conta_Com_Sessao_De_Outra_Mesa_Retorna_Nao_Encontrado()
    {
        var world = await SeedWorldAsync();
        var (tableA, sessionA, qrTokenA) = await OpenSessionAsync(world, "21", waiterId: null);
        var (tableB, sessionB, _) = await OpenSessionAsync(world, "22", waiterId: null);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        // qrToken da mesa A, mas sessionId/tableId da mesa B — nunca deveria bater.
        var result = await sender.Send(new RequestBillByQrCommand(qrTokenA, sessionB, tableB, "SINGLE", null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.InvalidTableToken);
    }

    /// <summary>Escalonamento (US-025 §4): chamada pendente há mais que o limiar escala para todos os garçons.</summary>
    [Fact]
    public async Task Escalonamento_Dispara_Quando_O_Limiar_E_Ultrapassado()
    {
        var world = await SeedWorldAsync();
        var ana = Guid.NewGuid();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "23", waiterId: ana);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        await provider.GetRequiredService<ISender>().Send(new CallWaiterCommand(qrToken, sessionId, tableId));

        // Backdate do RaisedAt para simular uma chamada pendente há mais tempo que o limiar —
        // Alert não expõe setter público (é gravado sempre com o relógio real na criação), então o
        // teste ajusta a coluna diretamente via SQL, sem contornar nenhuma regra de negócio.
        await using (var backdateDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var backdatedRaisedAt = DateTimeOffset.UtcNow - Nexora.Api.Edge.Workers.WaiterCallEscalationWorker.EscalationThreshold - TimeSpan.FromMinutes(1);
            await backdateDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE alert SET raised_at = {backdatedRaisedAt} WHERE entity_id = {sessionId} AND type = {AlertTypes.WaiterCalled}");
        }

        var escalationBroadcaster = new RecordingAlertsBroadcaster();
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(_ => _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)));
        services.AddSingleton<IAlertsBroadcaster>(escalationBroadcaster);
        var workerProvider = services.BuildServiceProvider();

        var worker = new Nexora.Api.Edge.Workers.WaiterCallEscalationWorker(
            workerProvider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Nexora.Api.Edge.Workers.WaiterCallEscalationWorker>.Instance);

        var escalatedCount = await worker.RunOnceAsync(CancellationToken.None);

        escalatedCount.Should().Be(1);
        escalationBroadcaster.WaiterCallEscalatedCalls.Should().ContainSingle(c => c.TableId == tableId);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alert = await verifyDb.Alerts.SingleAsync(a => a.EntityId == sessionId && a.Type == AlertTypes.WaiterCalled);
        alert.TargetRoles.Should().Contain("WAITER", "o alerta escala para os demais garçons do ambiente");
    }

    /// <summary>Escalonamento NÃO dispara antes do limiar ser ultrapassado.</summary>
    [Fact]
    public async Task Escalonamento_Nao_Dispara_Antes_Do_Limiar()
    {
        var world = await SeedWorldAsync();
        var ana = Guid.NewGuid();
        var (tableId, sessionId, qrToken) = await OpenSessionAsync(world, "24", waiterId: ana);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        await provider.GetRequiredService<ISender>().Send(new CallWaiterCommand(qrToken, sessionId, tableId));

        var escalationBroadcaster = new RecordingAlertsBroadcaster();
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(_ => _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)));
        services.AddSingleton<IAlertsBroadcaster>(escalationBroadcaster);
        var workerProvider = services.BuildServiceProvider();

        var worker = new Nexora.Api.Edge.Workers.WaiterCallEscalationWorker(
            workerProvider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Nexora.Api.Edge.Workers.WaiterCallEscalationWorker>.Instance);

        var escalatedCount = await worker.RunOnceAsync(CancellationToken.None);

        escalatedCount.Should().Be(0);
        escalationBroadcaster.WaiterCallEscalatedCalls.Should().BeEmpty();
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId);

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
        var store = Domain.Platform.Store.Create(storeId, tenantId, "Loja de teste", isDefault: true);
        storeDb.Stores.Add(store);
        var area = Area.Create(tenantId, storeId, "Salão de teste");
        storeDb.Areas.Add(area);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, area.Id);
    }

    private async Task<Guid> SeedProductAsync(Guid tenantId, string productName, string variantName, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, variantName);
        db.ProductVariants.Add(variant);

        var price = Price.Create(tenantId, variant.Id, Channel.DineIn, unitPrice);
        db.Prices.Add(price);

        await db.SaveChangesAsync();

        return variant.Id;
    }

    private async Task<(Guid TableId, Guid SessionId, string QrToken)> OpenSessionAsync(World world, string tableLabel, Guid? waiterId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var qrToken = $"qr-{tableLabel}-{Guid.NewGuid():N}";
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, tableLabel, qrToken, seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow),
            guestCount: 2, waiterId: waiterId, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return (table.Id, session.Id, qrToken);
    }
}
