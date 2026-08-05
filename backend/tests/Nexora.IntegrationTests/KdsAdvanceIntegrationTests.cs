using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices.Commands.UpdateDevicePreferences;
using Nexora.Application.Orders.Commands.AddItemToOrder;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.AdvanceKdsItem;
using Nexora.Application.Orders.Commands.AdvanceKdsOrder;
using Nexora.Application.Orders.Commands.UndoKdsItemAdvance;
using Nexora.Application.Orders.Queries.GetKdsQueue;
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
/// EPIC-E-04 (KDS Cozinha) contra PostgreSQL real (Testcontainers) — US-040 (limiar de cor na
/// fila), US-041 (avanço por código curto/item/lote, desfazer) e o pré-requisito de US-042/045/047
/// (preferências de dispositivo). Mesmo padrão de harness de <c>KdsRoutingIntegrationTests</c>
/// (US-031), propositalmente em arquivo separado para não competir por edição com aquele.
/// </summary>
[Collection("Postgres")]
public sealed class KdsAdvanceIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public KdsAdvanceIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>US-040 §5 — item recém-chegado (0 min decorrido) é NORMAL; limiar padrão do tenant é 12/18 min (TenantPrepTimeDefaults) quando a variação não define o próprio.</summary>
    [Fact]
    public async Task Fila_Devolve_ThresholdState_Normal_Para_Item_Recem_Chegado()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));

        var queue = await sender.Send(new GetKdsQueueQuery(stationId, null));

        queue.IsSuccess.Should().BeTrue();
        var item = queue.Value!.Items.Should().ContainSingle().Subject;
        item.ThresholdState.Should().Be("NORMAL");
        item.OrderId.Should().NotBeEmpty();
    }

    /// <summary>US-041 §7 — pedido de um item só: digitar o código repetidamente percorre os estados, um passo por vez (Batch=false).</summary>
    [Fact]
    public async Task AdvanceKdsOrder_Sem_Lote_Avanca_Um_Passo_Por_Vez()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Marguerita", "Grande", 45m, stationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == added.Value!.OrderId);

        var first = await sender.Send(new AdvanceKdsOrderCommand(order.ShortCode, stationId, Batch: false));
        first.IsSuccess.Should().BeTrue();
        first.Value!.Advanced.Should().ContainSingle(i => i.Status == "FIRED");

        var second = await sender.Send(new AdvanceKdsOrderCommand(order.ShortCode, stationId, Batch: false));
        second.IsSuccess.Should().BeTrue();
        second.Value!.Advanced.Should().ContainSingle(i => i.Status == "IN_OVEN");
    }

    /// <summary>US-041 §3 — confirmação de lote avança TODOS os itens ativos do pedido nesta praça de uma vez.</summary>
    [Fact]
    public async Task AdvanceKdsOrder_Com_Lote_Avanca_Todos_Os_Itens_Da_Praca()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Portuguesa", "Grande", 47m, stationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var firstItem = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        var secondItem = await sender.Send(new AddItemToOrderCommand(firstItem.Value!.OrderId, variantId, 1, null, null, null, null, null));
        secondItem.IsSuccess.Should().BeTrue(secondItem.Error);
        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == firstItem.Value.OrderId);

        var batch = await sender.Send(new AdvanceKdsOrderCommand(order.ShortCode, stationId, Batch: true));

        batch.IsSuccess.Should().BeTrue();
        batch.Value!.Advanced.Should().HaveCount(2);
        batch.Value.Advanced.Should().OnlyContain(i => i.Status == "FIRED");
    }

    /// <summary>US-041 §7 — código sem correspondência na praça devolve SHORT_CODE_NOT_FOUND (404), o mesmo cenário Gherkin "Código inexistente".</summary>
    [Fact]
    public async Task AdvanceKdsOrder_Com_Codigo_Inexistente_Devolve_Short_Code_Not_Found()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new AdvanceKdsOrderCommand("999", stationId, Batch: false));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.KdsShortCodeNotFound);
    }

    /// <summary>US-041 §7 — avanço direto por item (toque no cartão), sem precisar do orderId.</summary>
    [Fact]
    public async Task AdvanceKdsItem_Avanca_Pelo_Id_Do_Item_Sem_Precisar_Do_OrderId()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));

        var advanced = await sender.Send(new AdvanceKdsItemCommand(added.Value!.Id));

        advanced.IsSuccess.Should().BeTrue();
        advanced.Value!.Status.Should().Be("FIRED");
    }

    /// <summary>US-041 §3/§4 — desfazer dentro da janela reverte um passo sem apagar o evento original (domain_event append-only continua íntegro).</summary>
    [Fact]
    public async Task UndoKdsItemAdvance_Dentro_Da_Janela_Reverte_Um_Passo()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        await sender.Send(new AdvanceKdsItemCommand(added.Value!.Id));

        var eventsBeforeUndo = await db.DomainEvents.AsNoTracking().Where(e => e.AggregateId == added.Value.Id).CountAsync();

        var undone = await sender.Send(new UndoKdsItemAdvanceCommand(added.Value.Id));

        undone.IsSuccess.Should().BeTrue();
        undone.Value!.Status.Should().Be("QUEUED");

        var eventsAfterUndo = await db.DomainEvents.AsNoTracking().Where(e => e.AggregateId == added.Value.Id).CountAsync();
        eventsAfterUndo.Should().Be(eventsBeforeUndo + 1, "o evento original permanece e um evento de correção é somado, nunca substituído");
    }

    /// <summary>US-041 §4 — fora da janela de 10 s o desfazer é recusado (KDS_UNDO_WINDOW_EXPIRED).</summary>
    [Fact]
    public async Task UndoKdsItemAdvance_Apos_A_Janela_E_Recusado()
    {
        var world = await SeedWorldAsync();
        var stationId = await SeedStationAsync(world.TenantId, world.StoreId, "FORNO", "Forno");
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", 40m, stationId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        await sender.Send(new AdvanceKdsItemCommand(added.Value!.Id));

        // Empurra os dois carimbos (T0 e T1) 20s para o passado, preservando fired_at >= placed_at
        // (ck_item_sequence) — simula "a transição aconteceu há 20s" sem violar a constraint que um
        // AdvanceKdsItemCommand com OccurredAt retroativo (fired_at < placed_at, ambos "agora") violaria.
        var rewoundAt = DateTimeOffset.UtcNow.AddSeconds(-20);
        await db.OrderItems.Where(i => i.Id == added.Value.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.PlacedAt, rewoundAt).SetProperty(i => i.FiredAt, rewoundAt));
        // ExecuteUpdateAsync é SQL direto e não passa pelo change tracker — sem isto o handler
        // abaixo receberia de volta a entidade JÁ RASTREADA (com o FiredAt antigo) em vez de reler
        // do banco a linha que acabou de ser retrocedida.
        db.ChangeTracker.Clear();

        var undone = await sender.Send(new UndoKdsItemAdvanceCommand(added.Value.Id));

        undone.IsSuccess.Should().BeFalse();
        undone.Code.Should().Be(ApiErrorCodes.KdsUndoWindowExpired);
    }

    /// <summary>US-042/045/047 — o próprio dispositivo pode gravar sua preferência, e uma segunda chamada mescla em vez de substituir a chave irmã já gravada.</summary>
    [Fact]
    public async Task UpdateDevicePreferences_Mescla_Sem_Apagar_Chaves_Irmas_Ja_Gravadas()
    {
        var world = await SeedWorldAsync();
        var deviceId = await SeedDeviceAsync(world.TenantId, world.StoreId);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, deviceId: deviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var stationFilter = await sender.Send(new UpdateDevicePreferencesCommand(
            deviceId, """{"kds":{"stationIds":["11111111-1111-1111-1111-111111111111"]}}"""));
        stationFilter.IsSuccess.Should().BeTrue();

        var sound = await sender.Send(new UpdateDevicePreferencesCommand(
            deviceId, """{"kds":{"sound":{"enabled":true,"volume":0.8}}}"""));

        sound.IsSuccess.Should().BeTrue();
        var kds = sound.Value!.Preferences.GetProperty("kds");
        kds.GetProperty("stationIds").GetArrayLength().Should().Be(1, "a preferência gravada antes não pode ser apagada por uma mescla posterior de outra sub-chave");
        kds.GetProperty("sound").GetProperty("enabled").GetBoolean().Should().BeTrue();
    }

    /// <summary>Autoatendimento é permitido (o próprio terminal ajustando a si mesmo); alterar OUTRO dispositivo sem device:manage é recusado.</summary>
    [Fact]
    public async Task UpdateDevicePreferences_De_Outro_Dispositivo_Sem_Permissao_E_Recusado()
    {
        var world = await SeedWorldAsync();
        var deviceId = await SeedDeviceAsync(world.TenantId, world.StoreId);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, deviceId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new UpdateDevicePreferencesCommand(deviceId, """{"kds":{"sound":{"enabled":false}}}"""));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.AuthPermissionDenied);
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

    private async Task<Guid> SeedDeviceAsync(Guid tenantId, Guid storeId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var device = Device.Create(tenantId, storeId, "KDS Forno", DeviceType.Kds, $"fp-{Guid.NewGuid():N}");
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        return device.Id;
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
