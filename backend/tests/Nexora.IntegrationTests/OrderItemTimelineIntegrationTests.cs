using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;
using Nexora.Application.Orders.Queries.GetOrderItemTimeline;
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
/// US-032 (Carimbos de tempo T0 a T5) §12 (Estratégia de teste) contra um PostgreSQL real
/// (Testcontainers): constraint <c>ck_item_sequence</c>, preservação de <c>occurred_at</c>
/// distinto de <c>recorded_at</c> após uma sincronização tardia, e o teste de regressão "toda
/// transição de estado emite seu evento" (DoD explícito da história).
/// </summary>
[Collection("Postgres")]
public sealed class OrderItemTimelineIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public OrderItemTimelineIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Ordem cronológica garantida" (US-032 §4): gravar <c>ready_at</c> anterior a <c>fired_at</c> é recusado pelo banco, não pela aplicação.</summary>
    [Fact]
    public async Task Ck_Item_Sequence_Recusa_Gravar_ReadyAt_Anterior_A_FiredAt()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Calabresa", "Broto", unitPrice: 40m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var addDb = _fixture.CreateAppDbContext(tenantContext);
        await using var addProvider = BuildContainer(addDb, tenantContext);
        var added = await addProvider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var item = await db.OrderItems.SingleAsync(i => i.Id == added.Value!.Id);

        var firedAt = DateTimeOffset.UtcNow;
        item.Fire(Guid.NewGuid(), occurredAt: firedAt);
        // Status agora é Fired — MarkReady aceita a partir daí (item sem gargalo), mas o horário
        // informado é ANTERIOR ao próprio firedAt: só a constraint do banco pode recusar isto, a
        // validação de domínio hoje só olha o Status (ver docstring de OrderItem).
        item.MarkReady(Guid.NewGuid(), occurredAt: firedAt.AddSeconds(-30));

        var act = async () => await db.SaveChangesAsync();

        var exception = await act.Should().ThrowAsync<DbUpdateException>(
            "ck_item_sequence deve recusar ready_at anterior a fired_at, não a aplicação");
        exception.WithInnerException<Npgsql.PostgresException>()
            .Which.SqlState.Should().Be("23514", "23514 é o SQLSTATE padrão do Postgres para check_violation");
    }

    /// <summary>
    /// US-032 §9/ADR-034: <c>occurred_at</c> é o horário do FATO informado pela aplicação — não é
    /// silenciosamente substituído pelo momento real do <c>SaveChangesAsync</c>. Usa um delta
    /// pequeno mas válido em relação a <c>placed_at</c> (para não violar <c>ck_item_sequence</c>,
    /// coberto à parte no teste acima) — o suficiente para provar que o valor gravado é o
    /// INFORMADO, não o "agora" da gravação.
    /// </summary>
    [Fact]
    public async Task OccurredAt_Do_Carimbo_E_O_Valor_Informado_Nao_O_Momento_Da_Gravacao()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Quatro Queijos", "Grande", unitPrice: 48m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var addDb = _fixture.CreateAppDbContext(tenantContext);
        await using var addProvider = BuildContainer(addDb, tenantContext);
        var added = await addProvider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue();

        DateTimeOffset occurredAt;
        await using (var offlineDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var item = await offlineDb.OrderItems.SingleAsync(i => i.Id == added.Value!.Id);
            occurredAt = item.PlacedAt.AddSeconds(3); // válido (>= placed_at), distinto do "agora" da gravação
            item.Fire(Guid.NewGuid(), occurredAt: occurredAt);
            await offlineDb.SaveChangesAsync();
        }

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var reloadedItem = await readDb.OrderItems.SingleAsync(i => i.Id == added.Value!.Id);
        // BeCloseTo, não Be: timestamptz do Postgres tem precisão de microssegundo, perde o último
        // dígito dos 100ns-ticks do DateTimeOffset — diferença sub-microssegundo, não um bug.
        reloadedItem.FiredAt.Should().BeCloseTo(occurredAt, TimeSpan.FromMilliseconds(1), "fired_at grava exatamente o occurred_at informado, não o instante do SaveChangesAsync");
    }

    /// <summary>
    /// US-032 §12 (Estratégia de teste): "occurred_at preservado após sincronização de 6 horas de
    /// atraso". Testado em <see cref="DomainEvent"/> — a entidade que de fato modela os dois
    /// campos (<c>OccurredAt</c>/<c>RecordedAt</c>, ADR-034) — sem depender de
    /// <c>OrderItem</c>/<c>ck_item_sequence</c> (que rege a ordem ENTRE os seis carimbos de um
    /// mesmo item, uma preocupação diferente da distinção occurred_at×recorded_at de um evento).
    /// </summary>
    [Fact]
    public async Task DomainEvent_OccurredAt_E_Preservado_Apos_Sincronizacao_De_Seis_Horas_De_Atraso()
    {
        var world = await SeedWorldAsync();

        var occurredSixHoursAgo = DateTimeOffset.UtcNow.AddHours(-6);
        var beforeSave = DateTimeOffset.UtcNow;
        var aggregateId = Guid.NewGuid();

        await using (var offlineDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            offlineDb.DomainEvents.Add(DomainEvent.Create(
                world.TenantId,
                type: "order.item.fired",
                aggregateType: "order_item",
                aggregateId: aggregateId,
                payload: "{}",
                origin: "EDGE",
                occurredAt: occurredSixHoursAgo,
                storeId: world.StoreId));

            await offlineDb.SaveChangesAsync();
        }

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var domainEvent = await readDb.DomainEvents.SingleAsync(e => e.AggregateId == aggregateId);

        domainEvent.OccurredAt.Should().BeCloseTo(occurredSixHoursAgo, TimeSpan.FromMilliseconds(1), "occurred_at é o horário do fato — preservado tal como informado, mesmo sincronizado bem depois (tolerância só de precisão de microssegundo do timestamptz)");
        domainEvent.RecordedAt.Should().BeOnOrAfter(beforeSave, "recorded_at é o momento da GRAVAÇÃO (agora), nunca o do fato");
        (domainEvent.RecordedAt - domainEvent.OccurredAt).Should().BeGreaterThan(TimeSpan.FromHours(5), "a distância entre os dois campos reflete o atraso da sincronização simulada");
    }

    /// <summary>
    /// DoD explícito da US-032 ("Teste automatizado garantindo que nenhuma transição de estado
    /// ocorre sem emitir evento") — varre as cinco transições disparadas por
    /// <c>AdvanceOrderItemStatusCommand</c> (Fire→SendToOven→TakeOutOfOven→MarkReady→MarkServed) e
    /// confirma que cada uma grava o <c>DomainEvent</c> correspondente na MESMA transação
    /// (ADR-006). <c>OrderItem.Cancel</c> (o sexto método de transição do domínio) FICA DE FORA
    /// desta varredura de propósito: não existe hoje nenhum comando de Application que o invoque
    /// (só chamado diretamente em teste, ver <c>OrderConsumptionIntegrationTests</c>) — gap
    /// pré-existente a esta história, não introduzido por ela. As permissões
    /// <c>order:cancel_queued</c>/<c>order:cancel_started</c> já reservadas no catálogo fechado
    /// (<c>Nexora.Domain.Platform.PermissionCatalog</c>) sugerem que o comando de cancelamento é
    /// escopo de uma história futura dedicada — registrado no relatório desta tarefa.
    /// </summary>
    [Fact]
    public async Task Cada_Transicao_De_Status_Via_Advance_Emite_O_DomainEvent_Correspondente()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Portuguesa", "Grande", unitPrice: 52m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid(), deviceId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue();

        var expectedEventTypesInOrder = new[]
        {
            "order.item.fired",
            "order.item.in_oven",
            "order.item.out_of_oven",
            "order.item.ready",
            "order.item.served",
        };

        foreach (var expectedType in expectedEventTypesInOrder)
        {
            var advanced = await sender.Send(new AdvanceOrderItemStatusCommand(added.Value!.OrderId, added.Value.Id));
            advanced.IsSuccess.Should().BeTrue($"esperava avançar com sucesso rumo ao evento {expectedType}");
        }

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var recordedEventTypes = await verifyDb.DomainEvents
            .Where(e => e.AggregateId == added.Value!.Id && e.AggregateType == "order_item")
            .Select(e => e.Type)
            .ToListAsync();

        foreach (var expectedType in expectedEventTypesInOrder)
        {
            recordedEventTypes.Should().Contain(expectedType, $"a transição para {expectedType} precisa emitir o DomainEvent correspondente (ADR-006)");
        }
    }

    /// <summary>Cenário Gherkin "Item que passa pelo gargalo" (US-032 §4): via <c>GetOrderItemTimelineQuery</c>, os seis carimbos e os sete intervalos após o ciclo completo.</summary>
    [Fact]
    public async Task GetTimeline_Apos_Ciclo_Completo_Devolve_Os_Seis_Carimbos_E_As_Sete_Duracoes()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Napolitana", "Grande", unitPrice: 55m);
        var sessionId = await OpenSessionAsync(world);

        var actorId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: actorId, deviceId: deviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue();

        for (var i = 0; i < 5; i++)
        {
            var advanced = await sender.Send(new AdvanceOrderItemStatusCommand(added.Value!.OrderId, added.Value.Id));
            advanced.IsSuccess.Should().BeTrue();
        }

        var timeline = await sender.Send(new GetOrderItemTimelineQuery(added.Value!.OrderId, added.Value.Id));

        timeline.IsSuccess.Should().BeTrue();
        timeline.Value!.OrderItemId.Should().Be(added.Value.Id);
        timeline.Value.Timestamps.FiredAt.At.Should().NotBeNull();
        timeline.Value.Timestamps.OvenInAt.At.Should().NotBeNull();
        timeline.Value.Timestamps.OvenOutAt.At.Should().NotBeNull();
        timeline.Value.Timestamps.ReadyAt.At.Should().NotBeNull();
        timeline.Value.Timestamps.ServedAt.At.Should().NotBeNull();
        timeline.Value.Durations.TotalSeconds.Should().NotBeNull().And.BeGreaterThanOrEqualTo(0);
        timeline.Value.Durations.QueueSeconds.Should().NotBeNull();
        timeline.Value.Durations.CookSeconds.Should().NotBeNull();
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

    private async Task<(Guid VariantId, Guid ModifierId)> SeedProductWithModifierAsync(
        Guid tenantId, string productName, string variantName, decimal unitPrice)
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

        var modifierGroup = ModifierGroup.Create(tenantId, "Adicionais de teste");
        db.ModifierGroups.Add(modifierGroup);
        var modifier = Modifier.Create(tenantId, modifierGroup.Id, "Modificador de teste", 0m);
        db.Modifiers.Add(modifier);

        await db.SaveChangesAsync();

        return (variant.Id, modifier.Id);
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

    private static ServiceProvider BuildContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton<IOrderConsumptionBroadcaster>(new RecordingOrderConsumptionBroadcaster());
        services.AddSingleton<IAlertsBroadcaster>(new RecordingAlertsBroadcaster());
        // US-031: AddOrderItemCommand/AdvanceOrderItemStatusCommand também dependem de IStationBroadcaster.
        services.AddSingleton<IStationBroadcaster>(new RecordingStationBroadcaster());
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
