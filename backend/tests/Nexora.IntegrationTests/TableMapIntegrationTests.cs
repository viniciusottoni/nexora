using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Queries.GetTableMap;
using Nexora.Contracts.Tables;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Persistence;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-023 (mapa de mesas) contra um PostgreSQL real (Testcontainers, mesma
/// <see cref="PostgresFixture"/> das demais US) e o mesmo pipeline MediatR de produção —
/// "Visão do salão", "Ação pendente destacada", "Filtro por responsabilidade" e o cálculo de
/// valor consumido do §12 ("bate com a soma dos itens da sessão"). Não cobre WebSocket/SignalR
/// (isso é <c>Nexora.ApiTests.TableMapHubTests</c>, que sobe o host real) nem o polling do
/// frontend (isso é teste de componente/vitest, com fake timers — não dá para simular um timer
/// de UI aqui).
/// </summary>
[Collection("Postgres")]
public sealed class TableMapIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TableMapIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Mapa_Mostra_Status_Tempo_Valor_E_Mesa_Livre_Sem_Sessao()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var waiterId = await SeedWaiterAsync(tenantId);
        var variantId = await SeedVariantAsync(tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var area = Area.Create(tenantId, storeId, "Salão");
            seedDb.Areas.Add(area);

            var occupied = DiningTable.Create(tenantId, storeId, area.Id, "1", "qr-1", sortOrder: 1);
            occupied.Occupy();
            var free = DiningTable.Create(tenantId, storeId, area.Id, "2", "qr-2", sortOrder: 2);
            seedDb.DiningTables.AddRange(occupied, free);

            var session = TableSession.Create(tenantId, storeId, occupied.Id, Today(), guestCount: 4, waiterId: waiterId);
            seedDb.TableSessions.Add(session);
            await seedDb.SaveChangesAsync();

            // OpenedAt não é parametrizável em TableSession.Create (sempre "agora") — recuando o
            // valor via ChangeTracker para simular uma mesa aberta há 47 minutos, sem violar o
            // encapsulamento do agregado (mesma técnica usada para simular tempo decorrido em
            // teste de integração com propriedade de setter privado).
            seedDb.Entry(session).Property(s => s.OpenedAt).CurrentValue = UtcNow().AddMinutes(-47);
            await seedDb.SaveChangesAsync();

            var order = Order.Create(tenantId, storeId, Channel.DineIn, "A001", Today(), sessionId: session.Id);
            var item1 = OrderItem.Create(tenantId, order.Id, variantId, unitPrice: 30.00m);
            var item2 = OrderItem.Create(tenantId, order.Id, variantId, unitPrice: 25.00m);
            item2.MarkReady(waiterId);
            order.AddItem(item1);
            order.AddItem(item2);
            seedDb.Orders.Add(order);
            await seedDb.SaveChangesAsync();
        }

        var result = await SendAsync(tenantId, storeId, new GetTableMapQuery(MineOnly: false, TableMapSortBy.Label));

        result.IsSuccess.Should().BeTrue();
        var tables = result.Value!.Tables;
        tables.Should().HaveCount(2);

        var occupiedEntry = tables.Single(t => t.Label == "1");
        occupiedEntry.Area.Should().Be("Salão");
        occupiedEntry.Status.Should().Be("OCCUPIED");
        occupiedEntry.Session.Should().NotBeNull();
        occupiedEntry.Session!.GuestCount.Should().Be(4);
        occupiedEntry.Session.MinutesOpen.Should().BeInRange(46, 48);
        // "Cálculo de valor consumido bate com a soma dos itens da sessão" (US-023 §12).
        occupiedEntry.Session.Total.Should().Be(55.00m);
        occupiedEntry.Session.Waiter.Should().NotBeNull();
        occupiedEntry.Session.Waiter!.Id.Should().Be(waiterId);
        occupiedEntry.Flags.ItemsReadyToServe.Should().Be(1);
        occupiedEntry.Flags.BillRequested.Should().BeFalse();
        occupiedEntry.Flags.WaiterCalled.Should().BeFalse("US-025 ainda não existe — a flag fica hardcoded em false até a tabela de alerta de chamada ser desenhada");

        var freeEntry = tables.Single(t => t.Label == "2");
        freeEntry.Status.Should().Be("FREE");
        freeEntry.Session.Should().BeNull();
        freeEntry.Flags.ItemsReadyToServe.Should().Be(0);
    }

    [Fact]
    public async Task Valor_Consumido_Exclui_Item_Cancelado_Da_Soma()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var variantId = await SeedVariantAsync(tenantId);
        var managerId = Guid.NewGuid();

        Guid tableId;
        Guid sessionId;
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var area = Area.Create(tenantId, storeId, "Salão");
            seedDb.Areas.Add(area);
            var table = DiningTable.Create(tenantId, storeId, area.Id, "5", "qr-5");
            table.Occupy();
            seedDb.DiningTables.Add(table);
            tableId = table.Id;

            var session = TableSession.Create(tenantId, storeId, table.Id, Today());
            seedDb.TableSessions.Add(session);
            sessionId = session.Id;
            await seedDb.SaveChangesAsync();

            var order = Order.Create(tenantId, storeId, Channel.DineIn, "A002", Today(), sessionId: session.Id);
            var kept = OrderItem.Create(tenantId, order.Id, variantId, unitPrice: 40.00m);
            var cancelled = OrderItem.Create(tenantId, order.Id, variantId, unitPrice: 999.00m);
            cancelled.Cancel("Cliente desistiu do item", managerId);
            order.AddItem(kept);
            order.AddItem(cancelled);
            seedDb.Orders.Add(order);
            await seedDb.SaveChangesAsync();
        }

        var result = await SendAsync(tenantId, storeId, new GetTableMapQuery(MineOnly: false, TableMapSortBy.Label));

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value!.Tables.Single(t => t.Id == tableId);
        entry.Session!.Total.Should().Be(40.00m, "o item cancelado não deve entrar na soma do valor consumido, mesmo valendo R$ 999");
    }

    [Fact]
    public async Task Ordenacao_Por_Urgencia_Coloca_Conta_Solicitada_E_Item_Pronto_No_Topo()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var variantId = await SeedVariantAsync(tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var area = Area.Create(tenantId, storeId, "Salão");
            seedDb.Areas.Add(area);

            // Mesa 1: sem nenhum sinal — deveria ficar por último apesar do número menor.
            var quiet = DiningTable.Create(tenantId, storeId, area.Id, "1", "qr-quiet", sortOrder: 1);
            quiet.Occupy();
            // Mesa 2: pediu a conta — sinal mais urgente (peso 8).
            var billRequested = DiningTable.Create(tenantId, storeId, area.Id, "2", "qr-bill", sortOrder: 2);
            billRequested.Occupy();
            // Mesa 3: item pronto para levar — sinal intermediário (peso 2).
            var itemReady = DiningTable.Create(tenantId, storeId, area.Id, "3", "qr-ready", sortOrder: 3);
            itemReady.Occupy();
            seedDb.DiningTables.AddRange(quiet, billRequested, itemReady);

            var quietSession = TableSession.Create(tenantId, storeId, quiet.Id, Today());
            var billSession = TableSession.Create(tenantId, storeId, billRequested.Id, Today());
            var readySession = TableSession.Create(tenantId, storeId, itemReady.Id, Today());
            seedDb.TableSessions.AddRange(quietSession, billSession, readySession);
            await seedDb.SaveChangesAsync();

            billSession.RequestBill();
            await seedDb.SaveChangesAsync();

            var order = Order.Create(tenantId, storeId, Channel.DineIn, "A003", Today(), sessionId: readySession.Id);
            var ready = OrderItem.Create(tenantId, order.Id, variantId, unitPrice: 10.00m);
            ready.MarkReady(Guid.NewGuid());
            order.AddItem(ready);
            seedDb.Orders.Add(order);
            await seedDb.SaveChangesAsync();
        }

        var result = await SendAsync(tenantId, storeId, new GetTableMapQuery(MineOnly: false, TableMapSortBy.Urgency));

        result.IsSuccess.Should().BeTrue();
        var labelsInOrder = result.Value!.Tables.Select(t => t.Label).ToList();
        labelsInOrder.Should().ContainInOrder("2", "3", "1");

        var byLabel = await SendAsync(tenantId, storeId, new GetTableMapQuery(MineOnly: false, TableMapSortBy.Label));
        byLabel.Value!.Tables.Select(t => t.Label).Should().ContainInOrder("1", "2", "3");
    }

    [Fact]
    public async Task Filtro_Minhas_Mesas_So_Devolve_As_Do_Garcom_Autenticado()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var waiterA = await SeedWaiterAsync(tenantId, "Ana");
        var waiterB = await SeedWaiterAsync(tenantId, "Bruno");

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var area = Area.Create(tenantId, storeId, "Salão");
            seedDb.Areas.Add(area);

            var mineTable = DiningTable.Create(tenantId, storeId, area.Id, "10", "qr-10");
            mineTable.Occupy();
            var otherWaiterTable = DiningTable.Create(tenantId, storeId, area.Id, "11", "qr-11");
            otherWaiterTable.Occupy();
            var freeTable = DiningTable.Create(tenantId, storeId, area.Id, "12", "qr-12");
            seedDb.DiningTables.AddRange(mineTable, otherWaiterTable, freeTable);

            seedDb.TableSessions.Add(TableSession.Create(tenantId, storeId, mineTable.Id, Today(), waiterId: waiterA));
            seedDb.TableSessions.Add(TableSession.Create(tenantId, storeId, otherWaiterTable.Id, Today(), waiterId: waiterB));
            await seedDb.SaveChangesAsync();
        }

        var result = await SendAsync(tenantId, storeId, new GetTableMapQuery(MineOnly: true, TableMapSortBy.Label), waiterA);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tables.Should().ContainSingle().Which.Label.Should().Be("10");
    }

    [Fact]
    public async Task Mesa_Aberta_Ha_Mais_Tempo_Que_A_Media_Das_Sessoes_Fechadas_E_Sinalizada()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var area = Area.Create(tenantId, storeId, "Salão");
            seedDb.Areas.Add(area);

            // Duas sessões históricas FECHADAS (mesa desativada — não aparece no mapa, só serve
            // de insumo para a média, exatamente como o MetricAggregationWorker do ADR-012 faria
            // se já existisse) com 10 e 20 minutos de duração — média de 15 minutos.
            var historyTable = DiningTable.Create(tenantId, storeId, area.Id, "H1", "qr-h1");
            historyTable.SoftDelete();
            seedDb.DiningTables.Add(historyTable);
            await seedDb.SaveChangesAsync();
            await CreateClosedSessionAsync(seedDb, tenantId, storeId, historyTable.Id, TimeSpan.FromMinutes(10));
            await CreateClosedSessionAsync(seedDb, tenantId, storeId, historyTable.Id, TimeSpan.FromMinutes(20));

            // Mesa corrente aberta há 40 minutos — bem acima da média histórica de 15 minutos.
            var lateTable = DiningTable.Create(tenantId, storeId, area.Id, "20", "qr-late");
            lateTable.Occupy();
            var onTimeTable = DiningTable.Create(tenantId, storeId, area.Id, "21", "qr-ontime");
            onTimeTable.Occupy();
            seedDb.DiningTables.AddRange(lateTable, onTimeTable);

            var lateSession = TableSession.Create(tenantId, storeId, lateTable.Id, Today());
            var onTimeSession = TableSession.Create(tenantId, storeId, onTimeTable.Id, Today());
            seedDb.TableSessions.AddRange(lateSession, onTimeSession);
            await seedDb.SaveChangesAsync();

            seedDb.Entry(lateSession).Property(s => s.OpenedAt).CurrentValue = UtcNow().AddMinutes(-40);
            seedDb.Entry(onTimeSession).Property(s => s.OpenedAt).CurrentValue = UtcNow().AddMinutes(-5);
            await seedDb.SaveChangesAsync();
        }

        var result = await SendAsync(tenantId, storeId, new GetTableMapQuery(MineOnly: false, TableMapSortBy.Label));

        result.IsSuccess.Should().BeTrue();
        var late = result.Value!.Tables.Single(t => t.Label == "20");
        var onTime = result.Value!.Tables.Single(t => t.Label == "21");

        late.Flags.AboveAvgDuration.Should().BeTrue("40 min está acima da média histórica de 15 min calculada sobre as sessões fechadas (fallback do ADR-012 enquanto metric_daily.avg_stay_seconds não é populado)");
        onTime.Flags.AboveAvgDuration.Should().BeFalse();
    }

    private static async Task<Guid> CreateClosedSessionAsync(
        AppDbContext db, Guid tenantId, Guid storeId, Guid tableId, TimeSpan duration)
    {
        var session = TableSession.Create(tenantId, storeId, tableId, Today());
        db.TableSessions.Add(session);
        await db.SaveChangesAsync();

        session.RequestBill();
        session.MarkAsPaid(subtotal: 10m, discountAmount: 0m, serviceFeeAmount: 0m, totalAmount: 10m);
        session.Close();
        await db.SaveChangesAsync();

        var openedAt = UtcNow().AddDays(-1);
        db.Entry(session).Property(s => s.OpenedAt).CurrentValue = openedAt;
        db.Entry(session).Property(s => s.ClosedAt).CurrentValue = openedAt + duration;
        await db.SaveChangesAsync();

        return session.Id;
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

    private async Task<Guid> SeedWaiterAsync(Guid tenantId, string name = "Garçom de teste")
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var waiter = AppUser.Create(tenantId, name, email: null, passwordHash: null, pinHash: "hash-pin-irrelevante");
        db.Users.Add(waiter);
        await db.SaveChangesAsync();
        return waiter.Id;
    }

    private async Task<Guid> SeedVariantAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var category = Category.Create(tenantId, "Pizzas");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = Product.Create(tenantId, category.Id, "Pizza de teste");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var variant = ProductVariant.Create(tenantId, product.Id, "Única");
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();

        return variant.Id;
    }

    private async Task<Result<TableMapResponse>> SendAsync(
        Guid tenantId, Guid storeId, GetTableMapQuery query, Guid? userId = null)
    {
        var tenantContext = new StaticTenantContext(tenantId, storeId, userId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildMediatRContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        return await sender.Send(query);
    }

    private static ServiceProvider BuildMediatRContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}
