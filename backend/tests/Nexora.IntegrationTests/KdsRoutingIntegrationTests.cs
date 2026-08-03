using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;
using Nexora.Application.Orders.Commands.CreateOrder;
using Nexora.Application.Orders.Queries.GetKdsQueue;
using Nexora.Contracts.Operation;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Persistence;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-031 (Roteamento simultâneo para cozinha e caixa) contra um PostgreSQL real (Testcontainers) —
/// mesmo pipeline MediatR de produção (ADR-037). Cobre a parte que a suíte de <c>Nexora.ApiTests</c>
/// (<c>KdsHubTests</c>, salas reais de SignalR) não cobre: que os TRÊS handlers
/// (<c>CreateOrderCommand</c>/<c>AddOrderItemCommand</c>/<c>AdvanceOrderItemStatusCommand</c>) de
/// fato chamam <see cref="IStationBroadcaster"/> com o <c>stationId</c> CORRETO de cada item (a
/// informação que <c>SignalRStationBroadcaster</c> usa depois para filtrar salas), e que
/// <see cref="GetKdsQueueQuery"/> (fonte do fallback de polling E de <c>KdsHub.Resume</c> na
/// reconexão, ADR-011) devolve exatamente os itens ativos da praça pedida.
/// </summary>
[Collection("Postgres")]
public sealed class KdsRoutingIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public KdsRoutingIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Cenário Gherkin "Chegada ao KDS" (US-031 §4): pedido com itens de praças diferentes — cada
    /// item chega ao broadcaster com o <c>StationId</c> da PRÓPRIA praça (forno/bebidas), nunca
    /// misturado, e o pedido inteiro (as duas praças) é enviado no mesmo <c>OrderPlaced</c> — quem
    /// filtra por sala é <c>SignalRStationBroadcaster</c> (coberto por <c>KdsHubTests</c>), este
    /// teste garante que o INSUMO dessa filtragem (o <c>StationId</c> de cada item) está certo.
    /// </summary>
    [Fact]
    public async Task Pedido_Com_Itens_De_Pracas_Diferentes_Leva_Cada_Item_Com_A_Propria_Estacao()
    {
        var world = await SeedWorldAsync();
        var ovenStationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var beverageStationId = await SeedStationAsync(world.TenantId, world.StoreId, "BEBIDAS", "Bebidas");
        var pizzaVariantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", unitPrice: 40m, stationId: ovenStationId);
        var sodaVariantId = await SeedProductAsync(world.TenantId, "Refrigerante", "Lata", unitPrice: 8m, stationId: beverageStationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var stationBroadcaster = new RecordingStationBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, stationBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand(
            "DineIn",
            sessionId,
            new[]
            {
                new CreateOrderItemInput(pizzaVariantId, 1, "bem assada", null, null),
                new CreateOrderItemInput(sodaVariantId, 1, null, null, null),
            },
            null));

        result.IsSuccess.Should().BeTrue();

        stationBroadcaster.OrderPlacedCalls.Should().ContainSingle();
        var call = stationBroadcaster.OrderPlacedCalls.Single();
        call.Items.Should().HaveCount(2);
        call.Items.Should().ContainSingle(i => i.StationId == ovenStationId && i.ProductName.Contains("Pizza"));
        call.Items.Should().ContainSingle(i => i.StationId == beverageStationId && i.ProductName.Contains("Refrigerante"));
        call.TableId.Should().NotBeNull();
        call.Channel.Should().Be("DineIn");
    }

    /// <summary>Item lançado depois do pedido (comanda já aberta) também roteia para a própria praça — EVT-004.</summary>
    [Fact]
    public async Task Item_Lancado_Depois_Do_Pedido_Roteia_Para_A_Propria_Praca()
    {
        var world = await SeedWorldAsync();
        var ovenStationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var pizzaVariantId = await SeedProductAsync(world.TenantId, "Pizza Marguerita", "Grande", unitPrice: 45m, stationId: ovenStationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var stationBroadcaster = new RecordingStationBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, stationBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, pizzaVariantId, 1, null, null, null));

        added.IsSuccess.Should().BeTrue();
        stationBroadcaster.ItemQueuedCalls.Should().ContainSingle(call => call.Item.StationId == ovenStationId);
    }

    /// <summary>Avanço de status (Queued->Fired) notifica a PRÓPRIA praça, com o stationId do item.</summary>
    [Fact]
    public async Task Avancar_Status_Do_Item_Notifica_A_Propria_Estacao()
    {
        var world = await SeedWorldAsync();
        var ovenStationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var pizzaVariantId = await SeedProductAsync(world.TenantId, "Pizza Quatro Queijos", "Grande", unitPrice: 48m, stationId: ovenStationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var stationBroadcaster = new RecordingStationBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, stationBroadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, pizzaVariantId, 1, null, null, null));
        var advanced = await sender.Send(new AdvanceOrderItemStatusCommand(added.Value!.OrderId, added.Value.Id));

        advanced.IsSuccess.Should().BeTrue();
        advanced.Value!.Status.Should().Be("FIRED");
        stationBroadcaster.ItemStatusChangedCalls.Should().ContainSingle(
            call => call.OrderItemId == added.Value.Id && call.StationId == ovenStationId && call.Status == "FIRED");
    }

    /// <summary>
    /// Cenário Gherkin "Queda do WebSocket no KDS"/"Reconexão com recuperação" (US-031 §4) — a fila
    /// devolvida por <see cref="GetKdsQueueQuery"/> (fallback de polling E fonte de
    /// <c>KdsHub.Resume</c>) só traz itens da PRÓPRIA praça, ativos (nunca servido/cancelado), e é
    /// um SNAPSHOT completo — reconectar depois de qualquer intervalo (mesmo passando <c>since</c>
    /// antigo) devolve TODOS os itens ainda ativos, nunca perde nenhum.
    /// </summary>
    [Fact]
    public async Task Fila_Da_Praca_E_Snapshot_Completo_Que_Nunca_Perde_Item_Na_Reconexao()
    {
        var world = await SeedWorldAsync();
        var ovenStationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var beverageStationId = await SeedStationAsync(world.TenantId, world.StoreId, "BEBIDAS", "Bebidas");
        var pizzaVariantId = await SeedProductAsync(world.TenantId, "Pizza Napolitana", "Grande", unitPrice: 46m, stationId: ovenStationId);
        var sodaVariantId = await SeedProductAsync(world.TenantId, "Suco", "300ml", unitPrice: 9m, stationId: beverageStationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingStationBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var pizza = await sender.Send(new AddOrderItemCommand(sessionId, pizzaVariantId, 1, null, null, null));
        await sender.Send(new AddOrderItemCommand(sessionId, sodaVariantId, 1, null, null, null));

        // "Conecta" com um lastEventId de 40s atrás (cenário Gherkin: "ficou 40 segundos
        // desconectado") — como a fila é sempre um snapshot completo dos itens ATIVOS, o valor de
        // since é irrelevante para o resultado: nada fica ausente, por construção.
        var oldSince = DateTimeOffset.UtcNow.AddSeconds(-40).ToString("O");
        var firstLook = await sender.Send(new GetKdsQueueQuery(ovenStationId, oldSince));

        firstLook.IsSuccess.Should().BeTrue();
        firstLook.Value!.Items.Should().ContainSingle(i => i.OrderItemId == pizza.Value!.Id, "a fila do FORNO só traz o item da própria praça");
        firstLook.Value.Items.Should().NotContain(i => i.ProductName.Contains("Suco"), "item de Bebidas nunca aparece na fila do Forno");

        // "Reconecta" de novo, agora com o cursor mais recente devolvido pela chamada anterior —
        // continua trazendo o mesmo item (snapshot idempotente, cliente dedupe por orderItemId).
        var secondLook = await sender.Send(new GetKdsQueueQuery(ovenStationId, firstLook.Value.LastEventId));
        secondLook.IsSuccess.Should().BeTrue();
        secondLook.Value!.Items.Should().ContainSingle(i => i.OrderItemId == pizza.Value!.Id, "nenhum pedido some entre duas consultas seguidas");

        // Avança o item até SERVED — some da fila ativa (não é mais responsabilidade da praça).
        await sender.Send(new AdvanceOrderItemStatusCommand(pizza.Value!.OrderId, pizza.Value.Id)); // Fired
        await sender.Send(new AdvanceOrderItemStatusCommand(pizza.Value.OrderId, pizza.Value.Id)); // InOven
        await sender.Send(new AdvanceOrderItemStatusCommand(pizza.Value.OrderId, pizza.Value.Id)); // OutOfOven
        await sender.Send(new AdvanceOrderItemStatusCommand(pizza.Value.OrderId, pizza.Value.Id)); // Ready
        await sender.Send(new AdvanceOrderItemStatusCommand(pizza.Value.OrderId, pizza.Value.Id)); // Served

        var afterServed = await sender.Send(new GetKdsQueueQuery(ovenStationId, null));
        afterServed.IsSuccess.Should().BeTrue();
        afterServed.Value!.Items.Should().BeEmpty("item servido sai da fila ATIVA da praça");
    }

    /// <summary>Praça inexistente (ou de outro tenant) devolve 404 — nunca revela nada sobre outro tenant (RN-015/ADR-021).</summary>
    [Fact]
    public async Task Estacao_Inexistente_Devolve_Erro_Station_Not_Found()
    {
        var world = await SeedWorldAsync();

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingStationBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetKdsQueueQuery(Guid.NewGuid(), null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.StationNotFound);
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

    private async Task<Guid> OpenSessionAsync(World world, string tableLabel = "1", string qrToken = "qr-mesa-1")
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"{tableLabel}-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"{qrToken}-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }

    private static ServiceProvider BuildContainer(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, IStationBroadcaster stationBroadcaster)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton(stationBroadcaster);
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
