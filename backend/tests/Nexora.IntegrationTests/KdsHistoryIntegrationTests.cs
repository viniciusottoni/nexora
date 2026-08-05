using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Availability;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.AdvanceKdsItem;
using Nexora.Application.Orders.Queries.GetKdsHistory;
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
/// US-046 (Histórico do turno no KDS) contra PostgreSQL real (Testcontainers) — item concluído
/// aparece no turno corrente, busca por código curto/mesa, resumo do turno e a delimitação pelo dia
/// operacional (ADR-018, cenário Gherkin "Delimitação pelo dia operacional"). Arquivo dedicado
/// (não em <c>KdsAdvanceIntegrationTests.cs</c>) para não competir por edição com quem mantém
/// aquele arquivo nesta mesma onda em paralelo.
/// </summary>
[Collection("Postgres")]
public sealed class KdsHistoryIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public KdsHistoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Consulta por código" (na prática, o item precisa primeiro aparecer no histórico) — item SERVIDO no turno corrente aparece com todos os seus carimbos e o autor de T5.</summary>
    [Fact]
    public async Task Item_Concluido_No_Turno_Corrente_Aparece_No_Historico_Com_Carimbos_E_Autor()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);
        var operatorId = await SeedUserAsync(world.TenantId, "Operador da Cozinha");
        var sessionId = await OpenSessionAsync(world, tableLabel: "12");

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: operatorId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue();
        await AdvanceToServedAsync(sender, added.Value!.Id);

        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == added.Value.OrderId);

        var history = await sender.Send(new GetKdsHistoryQuery(stationId, null));

        history.IsSuccess.Should().BeTrue();
        var item = history.Value!.Items.Should().ContainSingle().Subject;
        item.OrderCode.Should().Be(order.ShortCode);
        item.Table.Should().Be("12");
        item.FiredAt.Should().NotBeNull();
        item.ReadyAt.Should().NotBeNull();
        item.PrepSeconds.Should().BeGreaterThanOrEqualTo(0);
        item.Operator.Should().NotBeNull();
        item.Operator!.Id.Should().Be(operatorId);
        item.Operator.Name.Should().Be("Operador da Cozinha");
        history.Value.Summary.Count.Should().Be(1);
    }

    /// <summary>Cenário Gherkin "Consulta por código" — buscar pelo código curto devolve só o pedido correspondente, mesmo com outro pedido concluído no mesmo turno.</summary>
    [Fact]
    public async Task Busca_Por_Codigo_Curto_Devolve_Apenas_O_Pedido_Correspondente()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Marguerita", "Grande", 45m, stationId);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var sessionA = await OpenSessionAsync(world, tableLabel: "5", qrToken: "qr-mesa-5");
        var addedA = await sender.Send(new AddOrderItemCommand(sessionA, variantId, 1, null, null, null));
        await AdvanceToServedAsync(sender, addedA.Value!.Id);
        var orderA = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == addedA.Value.OrderId);

        var sessionB = await OpenSessionAsync(world, tableLabel: "6", qrToken: "qr-mesa-6");
        var addedB = await sender.Send(new AddOrderItemCommand(sessionB, variantId, 1, null, null, null));
        await AdvanceToServedAsync(sender, addedB.Value!.Id);

        var history = await sender.Send(new GetKdsHistoryQuery(stationId, orderA.ShortCode));

        history.IsSuccess.Should().BeTrue();
        var item = history.Value!.Items.Should().ContainSingle().Subject;
        item.OrderCode.Should().Be(orderA.ShortCode);
        item.OrderId.Should().Be(orderA.Id);
    }

    /// <summary>Cenário Gherkin "Busca por mesa" — buscar pelo rótulo da mesa devolve os itens daquela mesa no turno, ignorando os de outra mesa.</summary>
    [Fact]
    public async Task Busca_Por_Mesa_Devolve_Apenas_Os_Itens_Daquela_Mesa()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Portuguesa", "Grande", 47m, stationId);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var sessionTable12 = await OpenSessionAsync(world, tableLabel: "12", qrToken: "qr-mesa-12");
        var addedTable12 = await sender.Send(new AddOrderItemCommand(sessionTable12, variantId, 1, null, null, null));
        await AdvanceToServedAsync(sender, addedTable12.Value!.Id);

        var sessionTable7 = await OpenSessionAsync(world, tableLabel: "7", qrToken: "qr-mesa-7");
        var addedTable7 = await sender.Send(new AddOrderItemCommand(sessionTable7, variantId, 1, null, null, null));
        await AdvanceToServedAsync(sender, addedTable7.Value!.Id);

        var history = await sender.Send(new GetKdsHistoryQuery(stationId, "12"));

        history.IsSuccess.Should().BeTrue();
        var item = history.Value!.Items.Should().ContainSingle().Subject;
        item.Table.Should().Be("12");
    }

    /// <summary>Cenário Gherkin "Resumo do turno" — contagem e tempo médio de produção batem com os itens efetivamente concluídos no turno.</summary>
    [Fact]
    public async Task Resumo_Do_Turno_Bate_Com_A_Contagem_E_O_Tempo_Medio_Reais()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        // Três itens com prep_seconds determinístico (300s/600s/900s, média 600s). A constraint
        // REAL do banco (ver OrderItemConfiguration.cs — mais estrita que o resumo do doc.
        // Domain/03-Operacao.md) exige `ready_at >= oven_out_at` E `served_at >= ready_at`; como o
        // avanço completo já grava oven_out_at/ready_at/served_at bem próximos entre si ("agora"),
        // empurrar ready_at PARA A FRENTE (como uma primeira tentativa fez) estoura served_at e
        // viola a constraint. Por isso aqui se rebobina PLACED_AT e FIRED_AT PARA TRÁS a partir do
        // ready_at natural — ready_at/oven_out_at/served_at ficam intocados (continuam coerentes
        // entre si), só o início do intervalo T1→T4 (fired_at) se afasta o suficiente para produzir
        // o prep_seconds desejado.
        int[] prepSecondsPerItem = [300, 600, 900];
        foreach (var prepSeconds in prepSecondsPerItem)
        {
            var session = await OpenSessionAsync(world, tableLabel: $"m-{prepSeconds}", qrToken: $"qr-{prepSeconds}");
            var added = await sender.Send(new AddOrderItemCommand(session, variantId, 1, null, null, null));
            await AdvanceToServedAsync(sender, added.Value!.Id);

            var item = await db.OrderItems.AsNoTracking().SingleAsync(i => i.Id == added.Value!.Id);
            var firedAt = item.ReadyAt!.Value.AddSeconds(-prepSeconds);
            var placedAt = firedAt.AddSeconds(-5);
            await db.OrderItems.Where(i => i.Id == added.Value!.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.PlacedAt, placedAt)
                    .SetProperty(i => i.FiredAt, firedAt));
        }
        db.ChangeTracker.Clear();

        var history = await sender.Send(new GetKdsHistoryQuery(stationId, null));

        history.IsSuccess.Should().BeTrue();
        history.Value!.Items.Should().HaveCount(3);
        history.Value.Summary.Count.Should().Be(3);
        history.Value.Summary.AvgPrepSeconds.Should().Be(600);
        history.Value.Items.Select(i => i.PrepSeconds).Should().BeEquivalentTo(prepSecondsPerItem);
    }

    /// <summary>
    /// Cenário Gherkin "Delimitação pelo dia operacional" — um pedido cujo <c>business_day</c> é de
    /// um turno ANTERIOR ao corrente não aparece no histórico, mesmo com o item já SERVIDO na mesma
    /// praça. <c>Order.BusinessDay</c> é reescrito diretamente (é onde <c>CreateOrderCommandHandler</c>
    /// materializa a virada, ADR-018) em vez de tentar recuar o relógio da máquina de teste — mesma
    /// técnica de rebobinar carimbo já usada em <c>KdsAdvanceIntegrationTests</c>.
    /// </summary>
    [Fact]
    public async Task Item_De_Outro_Dia_Operacional_Nao_Aparece_No_Historico_Do_Turno_Corrente()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var session = await OpenSessionAsync(world, tableLabel: "9", qrToken: "qr-mesa-9");
        var added = await sender.Send(new AddOrderItemCommand(session, variantId, 1, null, null, null));
        await AdvanceToServedAsync(sender, added.Value!.Id);

        var startHourUtc = BusinessDayPolicy.ResolveStartHourUtc(null);
        var currentBusinessDay = DateOnly.FromDateTime(
            BusinessDayPolicy.CurrentBusinessDayStart(DateTimeOffset.UtcNow, startHourUtc).UtcDateTime);
        var previousBusinessDay = currentBusinessDay.AddDays(-1);

        await db.Orders.Where(o => o.Id == added.Value.OrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.BusinessDay, previousBusinessDay));
        db.ChangeTracker.Clear();

        var history = await sender.Send(new GetKdsHistoryQuery(stationId, null));

        history.IsSuccess.Should().BeTrue();
        history.Value!.Items.Should().BeEmpty("o pedido pertence ao turno anterior, não ao corrente");
        history.Value.Summary.Count.Should().Be(0);

        // LIMITAÇÃO DOCUMENTADA: este teste não simula a VIRADA em si (relógio cruzando a hora
        // configurável de business_day_start_hour) — ele reescreve business_day diretamente, que é
        // exatamente o dado que a virada teria produzido. Simular a virada de verdade exigiria
        // congelar/injetar o relógio do servidor (IClock), inexistente hoje nesta solution
        // (BusinessDayPolicy.CurrentBusinessDayStart sempre recebe DateTimeOffset.UtcNow real na
        // Application) — fora do escopo desta história introduzir esse mecanismo.
    }

    /// <summary>Praça inexistente/de outro tenant devolve STATION_NOT_FOUND — mesma validação reaproveitada de <c>GetKdsQueueQueryHandler</c>.</summary>
    [Fact]
    public async Task Historico_De_Praca_Inexistente_Devolve_Station_Not_Found()
    {
        var world = await SeedWorldAsync();

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var history = await sender.Send(new GetKdsHistoryQuery(Guid.NewGuid(), null));

        history.IsSuccess.Should().BeFalse();
        history.Code.Should().Be(Nexora.Shared.Errors.ApiErrorCodes.StationNotFound);
    }

    private static async Task AdvanceToServedAsync(ISender sender, Guid itemId)
    {
        // Queued→Fired→InOven→OutOfOven→Ready→Served: cinco passos (OrderItemStatusMachine).
        for (var step = 0; step < 5; step++)
        {
            var advanced = await sender.Send(new AdvanceKdsItemCommand(itemId));
            advanced.IsSuccess.Should().BeTrue();
        }
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
        var store = Store.Create(storeId, tenantId, "Loja de teste", isDefault: true);
        storeDb.Stores.Add(store);
        var area = Area.Create(tenantId, storeId, "Salão de teste");
        storeDb.Areas.Add(area);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, area.Id);
    }

    private async Task<Guid> SeedStationAsync(Guid tenantId, Guid storeId, string code, string name)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var station = Station.Create(tenantId, storeId, code, name);
        db.Stations.Add(station);
        await db.SaveChangesAsync();
        return station.Id;
    }

    private async Task<Guid> SeedProductAsync(Guid tenantId, string productName, string variantName, decimal unitPrice, Guid? stationId = null)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName, stationId: stationId);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, variantName);
        db.ProductVariants.Add(variant);

        var price = Price.Create(tenantId, variant.Id, Channel.DineIn, unitPrice);
        db.Prices.Add(price);

        await db.SaveChangesAsync();

        return variant.Id;
    }

    private async Task<Guid> SeedUserAsync(Guid tenantId, string name)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var user = AppUser.Create(tenantId, name, email: null, passwordHash: null, pinHash: "hash-irrelevante-para-o-teste");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// Ao contrário do helper equivalente de <c>KdsAdvanceIntegrationTests</c> (que sufixa o rótulo
    /// com um Guid para não colidir entre chamadas), aqui o <paramref name="tableLabel"/> é gravado
    /// tal e qual: os testes de busca por mesa precisam de um rótulo EXATO e previsível para
    /// comparar contra a resposta (<c>item.Table.Should().Be("12")</c>). Cada teste usa rótulos
    /// distintos por chamada, e cada teste tem seu próprio tenant/loja (<see cref="SeedWorldAsync"/>),
    /// então não há colisão entre execuções.
    /// </summary>
    private async Task<Guid> OpenSessionAsync(World world, string tableLabel = "1", string qrToken = "qr-mesa-1")
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, tableLabel, $"{qrToken}-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton<IStationBroadcaster>(new RecordingStationBroadcaster());
        services.AddSingleton<IOrderConsumptionBroadcaster>(new RecordingOrderConsumptionBroadcaster());
        services.AddSingleton<IAlertsBroadcaster>(new RecordingAlertsBroadcaster());
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
