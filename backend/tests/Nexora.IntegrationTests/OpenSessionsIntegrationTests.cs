using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Queries.GetOpenSessions;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Persistence;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-050 (Painel de mesas e comandas abertas) contra um PostgreSQL real
/// (Testcontainers) — mesmo pipeline MediatR de produção (ADR-037), mesmo padrão de seed de
/// <c>BillSplitIntegrationTests</c> (US-027). Cobre os cenários exigidos pela estratégia de teste
/// da história (US-050 §12): "Visão de todas as comandas"/"Totalizador bate com a soma das
/// sessões", "Prioridade de conta solicitada" e busca/ordenação por mesa.
/// </summary>
[Collection("Postgres")]
public sealed class OpenSessionsIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public OpenSessionsIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Gherkin "Visão de todas as comandas" + estratégia de teste "Totalizador bate com a soma das sessões" (US-050 §12).</summary>
    [Fact]
    public async Task Retorna_Sessoes_Abertas_Com_Valor_Tempo_Garcom_E_Totalizador_Correto()
    {
        var world = await SeedWorldAsync();
        var waiterId = await SeedWaiterAsync(world.TenantId, "Ana");
        var pizza = await SeedProductAsync(world.TenantId, "Pizza Marguerita", "Média", 40m);
        var refrigerante = await SeedProductAsync(world.TenantId, "Refrigerante", "Lata", 8m);

        var sessionA = await OpenSessionAsync(world, "1", waiterId: waiterId, guestCount: 2);
        var sessionB = await OpenSessionAsync(world, "2", waiterId: null, guestCount: 4);

        await AddItemAsync(world, sessionA, pizza);
        await AddItemAsync(world, sessionB, refrigerante);
        await AddItemAsync(world, sessionB, refrigerante);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetOpenSessionsQuery(null, GetOpenSessionsSortBy.Table));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sessions.Should().HaveCount(2);

        var entryA = result.Value.Sessions.Single(s => s.SessionId == sessionA);
        entryA.Table.Should().Be("1");
        entryA.Area.Should().Be("Salão de teste");
        entryA.Total.Should().Be(40m);
        entryA.GuestCount.Should().Be(2);
        entryA.Waiter.Should().NotBeNull();
        entryA.Waiter!.Name.Should().Be("Ana");
        entryA.Status.Should().Be("OPEN");

        var entryB = result.Value.Sessions.Single(s => s.SessionId == sessionB);
        entryB.Total.Should().Be(16m, "duas unidades de Refrigerante a R$8");
        entryB.Waiter.Should().BeNull("sessão B não tem garçom atribuído");

        // Estratégia de teste US-050 §12: o totalizador do salão bate com a soma das sessões.
        result.Value.Summary.OpenSessions.Should().Be(2);
        result.Value.Summary.TotalOpen.Should().Be(result.Value.Sessions.Sum(s => s.Total));
        result.Value.Summary.TotalOpen.Should().Be(56m);
    }

    /// <summary>Gherkin "Prioridade de conta solicitada": contas pedidas aparecem no topo, ordenadas por tempo de espera decrescente, com o tempo de espera exibido.</summary>
    [Fact]
    public async Task Prioridade_De_Conta_Solicitada_Aparece_No_Topo_Ordenada_Por_Espera()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Grande", 50m);

        var openSession = await OpenSessionAsync(world, "1");
        var requestedFirst = await OpenSessionAsync(world, "2"); // pede a conta primeiro -> espera por mais tempo
        var requestedSecond = await OpenSessionAsync(world, "3"); // pede a conta depois -> espera por menos tempo

        await AddItemAsync(world, openSession, produto);
        await AddItemAsync(world, requestedFirst, produto);
        await AddItemAsync(world, requestedSecond, produto);

        await RequestBillDirectlyAsync(world, requestedFirst);
        await Task.Delay(1500); // garante uma diferença perceptível (segundos inteiros) de waitingSeconds entre as duas
        await RequestBillDirectlyAsync(world, requestedSecond);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetOpenSessionsQuery(null, GetOpenSessionsSortBy.Urgency));

        result.IsSuccess.Should().BeTrue();
        var ordered = result.Value!.Sessions;
        ordered.Should().HaveCount(3);

        // As duas com conta pedida aparecem no topo — a mesa aberta (sem conta pedida) vai para o final.
        ordered[0].Status.Should().Be("BILL_REQUESTED");
        ordered[1].Status.Should().Be("BILL_REQUESTED");
        ordered[2].SessionId.Should().Be(openSession);
        ordered[2].Status.Should().Be("OPEN");
        ordered[2].WaitingSeconds.Should().BeNull("só é aplicável quando a conta foi pedida — US-050 §7");

        // Quem pediu a conta PRIMEIRO está esperando há MAIS tempo — aparece primeiro (ordenação por espera decrescente).
        ordered[0].SessionId.Should().Be(requestedFirst);
        ordered[1].SessionId.Should().Be(requestedSecond);
        ordered[0].WaitingSeconds.Should().NotBeNull();
        ordered[1].WaitingSeconds.Should().NotBeNull();
        ordered[0].WaitingSeconds!.Value.Should().BeGreaterThan(ordered[1].WaitingSeconds!.Value);
    }

    /// <summary>US-050 §10 "Busca com foco automático": filtra por mesa, mas o totalizador continua refletindo o salão inteiro (não o resultado filtrado).</summary>
    [Fact]
    public async Task Busca_Por_Mesa_Filtra_A_Lista_Mas_Totalizador_Reflete_O_Salao_Inteiro()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Portuguesa", "Grande", 70m);

        var mesa7 = await OpenSessionAsync(world, "7");
        var mesa12 = await OpenSessionAsync(world, "12");
        await AddItemAsync(world, mesa7, produto);
        await AddItemAsync(world, mesa12, produto);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetOpenSessionsQuery("7", GetOpenSessionsSortBy.Table));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sessions.Should().ContainSingle().Which.SessionId.Should().Be(mesa7);
        // Totalizador continua sendo o do salão INTEIRO (duas sessões), não só do resultado filtrado.
        result.Value.Summary.OpenSessions.Should().Be(2);
        result.Value.Summary.TotalOpen.Should().Be(140m);
    }

    /// <summary>[DECISÃO] "Comanda" = short_code do pedido mais recente da sessão (ver docstring de OpenSessionEntryResponse.OrderCode) — a busca também encontra a mesa por esse código.</summary>
    [Fact]
    public async Task Busca_Por_Comanda_Encontra_A_Sessao_Pelo_Short_Code_Do_Pedido()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Napolitana", "Grande", 90m);

        var mesa5 = await OpenSessionAsync(world, "5");
        var outraMesa = await OpenSessionAsync(world, "8");
        await AddItemAsync(world, mesa5, produto);
        await AddItemAsync(world, outraMesa, produto);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var orderCode = (await readDb.Orders.SingleAsync(o => o.SessionId == mesa5)).ShortCode;
        orderCode.Should().NotBeNullOrWhiteSpace();

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetOpenSessionsQuery(orderCode, GetOpenSessionsSortBy.Table));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sessions.Should().ContainSingle().Which.SessionId.Should().Be(mesa5);
    }

    /// <summary>US-050 §10 "ordenação... com alternativa por número de mesa" — <c>sortBy=table</c> ordena pelo rótulo, não pela urgência.</summary>
    [Fact]
    public async Task SortBy_Table_Ordena_Pelo_Rotulo_Da_Mesa_Ignorando_Urgencia()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Mussarela", "Média", 45m);

        var mesa9 = await OpenSessionAsync(world, "9");
        var mesa2 = await OpenSessionAsync(world, "2");
        await AddItemAsync(world, mesa9, produto);
        await AddItemAsync(world, mesa2, produto);
        await RequestBillDirectlyAsync(world, mesa9); // mesmo com conta pedida (urgência máxima), sortBy=table não prioriza

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetOpenSessionsQuery(null, GetOpenSessionsSortBy.Table));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sessions.Select(s => s.Table).Should().ContainInOrder("2", "9");
    }

    /// <summary>US-050 §7 "pendingItems": conta itens ainda não servidos — item Served não entra na contagem (mesma definição de PendingItemsClosePolicy, US-035).</summary>
    [Fact]
    public async Task PendingItems_Conta_Itens_Ainda_Nao_Servidos()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Frango", "Grande", 55m);
        var sessionId = await OpenSessionAsync(world, "4");

        var actorId = Guid.NewGuid();
        var itemA = await AddItemAsync(world, sessionId, produto);
        await AddItemAsync(world, sessionId, produto); // permanece pendente (Queued) — item B do comentário abaixo

        // Percorre a fila do KDS até SERVED só para o item A — item B permanece pendente (Queued).
        await using (var writeDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var item = await writeDb.OrderItems.SingleAsync(i => i.Id == itemA);
            item.Fire(actorId);
            item.SendToOven(null);
            item.TakeOutOfOven();
            item.MarkReady(actorId);
            item.MarkServed(actorId);
            await writeDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetOpenSessionsQuery(null, GetOpenSessionsSortBy.Table));

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value!.Sessions.Single(s => s.SessionId == sessionId);
        entry.PendingItems.Should().Be(1, "item A foi servido; o segundo item continua pendente");
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId);

    private async Task<World> SeedWorldAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Domain.Platform.Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
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

    private async Task<Guid> SeedWaiterAsync(Guid tenantId, string name)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var waiter = AppUser.Create(tenantId, name, email: null, passwordHash: null, pinHash: "000000");
        db.Users.Add(waiter);
        await db.SaveChangesAsync();
        return waiter.Id;
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

    private async Task<Guid> OpenSessionAsync(World world, string tableLabel, Guid? waiterId = null, short guestCount = 2)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        // Cada teste semeia um tenant novo (SeedWorldAsync gera um Guid novo por chamada), então o
        // rótulo pedido pelo teste (ex. "2", "9") nunca colide com outra mesa dentro do mesmo
        // tenant — sem precisar do sufixo único + rename que BillSplitIntegrationTests usa (lá o
        // valor do rótulo em si é irrelevante para as asserções; aqui a ordenação por número de
        // mesa depende dele ser exatamente o que o teste pediu).
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, tableLabel, $"qr-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow),
            guestCount: guestCount, waiterId: waiterId, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }

    private async Task<Guid> AddItemAsync(World world, Guid sessionId, Guid variantId)
    {
        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        result.IsSuccess.Should().BeTrue();
        return result.Value!.Id;
    }

    /// <summary>Move a sessão para BILL_REQUESTED sem passar pelo comando completo (mesmo padrão de <c>BillSplitIntegrationTests.RequestBillDirectlyAsync</c>) — evita alertas alheios a este teste.</summary>
    private async Task RequestBillDirectlyAsync(World world, Guid sessionId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await db.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.RequestBill("SINGLE", null);
        await db.SaveChangesAsync();
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton<IAlertsBroadcaster>(new RecordingAlertsBroadcaster());
        services.AddSingleton<ITableMapBroadcaster>(new RecordingTableMapBroadcaster());
        services.AddSingleton<IOrderConsumptionBroadcaster>(new RecordingOrderConsumptionBroadcaster());
        services.AddSingleton<IStationBroadcaster>(new RecordingStationBroadcaster());
        services.AddSingleton<IAuthorizationTokenValidator>(new StubAuthorizationTokenValidator());
        if (db is AppDbContext appDbContext)
        {
            services.AddSingleton<IOrderShortCodeAllocator>(new OrderShortCodeAllocator(appDbContext));
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
