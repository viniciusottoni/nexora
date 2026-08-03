using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;
using Nexora.Application.Orders.Commands.CancelOrder;
using Nexora.Application.Orders.Commands.CancelOrderItem;
using Nexora.Application.Orders.Queries.GetCurrentSessionConsumption;
using Nexora.Domain.Catalog;
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
/// Cenários Gherkin da US-033 (Cancelar item ou pedido com autorização) contra um PostgreSQL real
/// (Testcontainers), mesmo pipeline MediatR de produção (ADR-037) — arquivo NOVO, dedicado a
/// <c>CancelOrderItemCommand</c>/<c>CancelOrderCommand</c> (o gap confirmado pelo relatório de
/// US-030/US-032: <see cref="OrderItem.Cancel"/>/<see cref="Order.Cancel"/> já existiam no domínio,
/// mas nenhum comando de Application os chamava em produção).
/// </summary>
[Collection("Postgres")]
public sealed class CancelOrderIntegrationTests
{
    private const string TestJwtSecret = "cancel-order-integration-test-jwt-secret-32-bytes!!";
    private const string TestPinLookupPepper = "cancel-order-integration-test-pin-lookup-pepper-32b!!";

    private readonly PostgresFixture _fixture;

    public CancelOrderIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Cancelamento antes do início da produção" (US-033 §4).</summary>
    [Fact]
    public async Task Cancelar_Item_Em_Fila_Nao_Exige_Autorizacao_E_Sai_Da_Fila_E_Do_Total_Da_Mesa()
    {
        var world = await SeedWorldAsync();
        var variantId = await SeedProductAsync(world.TenantId, "Refrigerante", unitPrice: 8m);
        var sessionId = await OpenSessionAsync(world);
        var (orderId, itemId) = await SeedOrderWithSingleItemAsync(world, sessionId, variantId, unitPrice: 8m, itemStatus: OrderItemStatus.Queued);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingOrderConsumptionBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CancelOrderItemCommand(orderId, itemId, "CUSTOMER_REQUEST", "cliente desistiu", AuthorizationToken: null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Item.Status.Should().Be("CANCELLED");
        result.Value.Item.WasStarted.Should().BeFalse();
        result.Value.Item.AuthorizedBy.Should().BeNull();
        broadcaster.ItemStatusChangedCalls.Should().ContainSingle(call => call.OrderItemId == itemId && call.Status == "CANCELLED");

        await using var assertDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        (await assertDb.DomainEvents.Where(e => e.Type == "order.item.cancelled" && e.AggregateId == itemId).ToListAsync())
            .Should().ContainSingle();

        // "sai do total da mesa" (US-024, mesmo padrão de OrderConsumptionIntegrationTests).
        var consumerContext = new StaticTenantContext(world.TenantId, world.StoreId, sessionId: sessionId);
        await using var consumerDb = _fixture.CreateAppDbContext(consumerContext);
        await using var consumerProvider = BuildContainer(consumerDb, consumerContext, new RecordingOrderConsumptionBroadcaster());
        var consumption = await consumerProvider.GetRequiredService<ISender>().Send(new GetCurrentSessionConsumptionQuery());

        consumption.IsSuccess.Should().BeTrue();
        consumption.Value!.Subtotal.Should().Be(0m, "o único item da mesa foi cancelado");
    }

    /// <summary>Cenário Gherkin "Cancelamento após início de produção" (parte 1) — sem token, recusa com 403.</summary>
    [Fact]
    public async Task Cancelar_Item_Iniciado_Sem_Token_E_Recusado_Com_403_E_Registra_Auditoria()
    {
        var world = await SeedWorldAsync();
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa", unitPrice: 45m);
        var sessionId = await OpenSessionAsync(world);
        var (orderId, itemId) = await SeedOrderWithSingleItemAsync(world, sessionId, variantId, unitPrice: 45m, itemStatus: OrderItemStatus.Fired);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CancelOrderItemCommand(orderId, itemId, "CUSTOMER_REQUEST", null, AuthorizationToken: null));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
        result.Errors!["action"].Should().Contain("CANCEL_STARTED_ITEM");
        result.Errors!["itemStatus"].Should().Contain("FIRED");

        await using var assertDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var item = await assertDb.OrderItems.SingleAsync(i => i.Id == itemId);
        item.Status.Should().Be(OrderItemStatus.Fired, "a recusa não muda o estado do item");

        (await assertDb.AuditLogs.Where(a => a.Action == "ORDER_ITEM_CANCEL_DENIED" && a.EntityId == itemId).ToListAsync())
            .Should().ContainSingle("US-033 §4, cenário 'Autorização negada': a tentativa deve ser registrada em audit_log");
    }

    /// <summary>Cenário Gherkin "Cancelamento após início de produção" (parte 2) — token expirado é recusado.</summary>
    [Fact]
    public async Task Cancelar_Item_Iniciado_Com_Token_Expirado_E_Recusado()
    {
        var world = await SeedWorldAsync();
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Frango", unitPrice: 50m);
        var sessionId = await OpenSessionAsync(world);
        var (orderId, itemId) = await SeedOrderWithSingleItemAsync(world, sessionId, variantId, unitPrice: 50m, itemStatus: OrderItemStatus.InOven);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());

        var tokenIssuer = provider.GetRequiredService<ITokenIssuer>();
        // ttlSeconds negativo — token nasce expirado há muito mais que os 120s de validade
        // (AuthTokenTtlSeconds.Authorization), mesmo truque de AuthorizationTokenValidatorTests.
        var expiredToken = await tokenIssuer.IssueAuthorizationTokenAsync(
            new Dictionary<string, object>
            {
                ["sub"] = world.WaiterId.ToString(),
                ["tid"] = world.TenantId.ToString(),
                ["sid"] = world.StoreId.ToString(),
                ["did"] = world.DeviceId.ToString(),
                ["action"] = "CANCEL_STARTED_ITEM",
                ["contextHash"] = "hash-nao-importa-o-token-ja-esta-expirado",
                ["authorizedBy"] = Guid.NewGuid().ToString(),
            },
            ttlSeconds: -300);

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new CancelOrderItemCommand(orderId, itemId, "CUSTOMER_REQUEST", null, expiredToken));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
    }

    /// <summary>
    /// Cenário Gherkin "Cancelamento após início de produção" (parte 3, fluxo completo) +
    /// "Registro completo na auditoria" — passa pelo MESMO caminho de produção: garçom pede a
    /// autorização de verdade (<c>AuthorizeSensitiveActionCommand</c>, PIN real do gerente) e
    /// repete o cancelamento com o token emitido.
    /// </summary>
    [Fact]
    public async Task Cancelar_Item_Iniciado_Com_Autorizacao_Valida_Cancela_E_Registra_Executor_E_Autorizador()
    {
        var world = await SeedWorldAsync();
        const string managerPin = "9911";
        var managerId = await SeedManagerWithPinAsync(world, managerPin);

        var variantId = await SeedProductAsync(world.TenantId, "Pizza Portuguesa", unitPrice: 52m);
        var sessionId = await OpenSessionAsync(world);
        var (orderId, itemId) = await SeedOrderWithSingleItemAsync(world, sessionId, variantId, unitPrice: 52m, itemStatus: OrderItemStatus.Ready);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingOrderConsumptionBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var authorized = await sender.Send(new AuthorizeSensitiveActionCommand(
            "CANCEL_STARTED_ITEM", managerPin, new Dictionary<string, object?> { ["orderItemId"] = itemId.ToString() }));

        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);
        authorized.Value!.AuthorizedBy.Id.Should().Be(managerId);

        var result = await sender.Send(new CancelOrderItemCommand(
            orderId, itemId, "CUSTOMER_REQUEST", "cliente desistiu", authorized.Value.AuthorizationToken));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Item.Status.Should().Be("CANCELLED");
        result.Value.Item.WasStarted.Should().BeTrue("o item estava READY — já passou por FIRED");
        result.Value.Item.AuthorizedBy.Should().NotBeNull();
        result.Value.Item.AuthorizedBy!.Id.Should().Be(managerId);
        broadcaster.ItemStatusChangedCalls.Should().ContainSingle(call => call.OrderItemId == itemId && call.Status == "CANCELLED");

        await using var assertDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var auditLog = await assertDb.AuditLogs.SingleAsync(a => a.Action == "ORDER_ITEM_CANCELLED" && a.EntityId == itemId);
        auditLog.ActorId.Should().Be(world.WaiterId, "executor");
        auditLog.AuthorizedBy.Should().Be(managerId, "autorizador");
        auditLog.DeviceId.Should().Be(world.DeviceId);
        auditLog.Reason.Should().Be("CUSTOMER_REQUEST");
        auditLog.After.Should().Contain("52"); // valor do item cancelado

        var domainEvent = await assertDb.DomainEvents.SingleAsync(e => e.Type == "order.item.cancelled" && e.AggregateId == itemId);
        domainEvent.Payload.Should().Contain("\"wasStarted\": true");
        domainEvent.AuthorizedBy.Should().Be(managerId);
    }

    /// <summary>Cenário Gherkin "Pedido fechado não cancela" (US-033 §4).</summary>
    [Fact]
    public async Task Cancelar_Pedido_Fechado_E_Recusado_Com_409_Apontando_Estorno()
    {
        var world = await SeedWorldAsync();
        var variantId = await SeedProductAsync(world.TenantId, "Suco", unitPrice: 9m);
        var sessionId = await OpenSessionAsync(world);
        var orderId = await SeedClosedOrderAsync(world, sessionId, variantId, unitPrice: 9m);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CancelOrderCommand(orderId, "CUSTOMER_REQUEST", null, AuthorizationToken: null));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.InvalidStateTransition);
        result.Error.Should().Contain("estorno", "a orientação deve apontar o fluxo de estorno (RF-CXA-13/Fase 2)");
    }

    /// <summary>Cenário Gherkin "Cancelamento de pedido inteiro" — três itens, um já iniciado: exige autorização e cancela todos na mesma operação.</summary>
    [Fact]
    public async Task Cancelar_Pedido_Com_Item_Iniciado_Exige_Autorizacao_E_Cancela_Todos_Os_Itens()
    {
        var world = await SeedWorldAsync();
        const string managerPin = "4321";
        _ = await SeedManagerWithPinAsync(world, managerPin);

        var variantId = await SeedProductAsync(world.TenantId, "Pizza Meio a Meio", unitPrice: 50m);
        var sessionId = await OpenSessionAsync(world);
        var (orderId, itemIds) = await SeedOrderWithThreeItemsAsync(world, sessionId, variantId, unitPrice: 50m);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingOrderConsumptionBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var withoutToken = await sender.Send(new CancelOrderCommand(orderId, "CUSTOMER_REQUEST", null, AuthorizationToken: null));
        withoutToken.IsFailure.Should().BeTrue();
        withoutToken.Code.Should().Be(ApiErrorCodes.AuthorizationRequired, "um dos três itens já foi iniciado");

        var authorized = await sender.Send(new AuthorizeSensitiveActionCommand(
            "CANCEL_STARTED_ITEM", managerPin, new Dictionary<string, object?> { ["orderId"] = orderId.ToString() }));
        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);

        var result = await sender.Send(new CancelOrderCommand(orderId, "CUSTOMER_REQUEST", null, authorized.Value!.AuthorizationToken));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Order.Status.Should().Be("CANCELLED");
        result.Value.Order.Items.Should().HaveCount(3);
        result.Value.Order.Items.Should().OnlyContain(i => i.Status == "CANCELLED");

        await using var assertDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        foreach (var itemId in itemIds)
        {
            var item = await assertDb.OrderItems.SingleAsync(i => i.Id == itemId);
            item.Status.Should().Be(OrderItemStatus.Cancelled);
        }

        (await assertDb.DomainEvents.Where(e => e.Type == "order.cancelled" && e.AggregateId == orderId).ToListAsync())
            .Should().ContainSingle();
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId, Guid WaiterId, Guid DeviceId);

    private async Task<World> SeedWorldAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var waiterId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        storeDb.Areas.Add(Area.Create(tenantId, storeId, "Salão de teste"));
        // Device REAL (não um Guid solto): AuthorizeSensitiveActionCommandHandler grava
        // AuthAttempt.DeviceId, e auth_attempt tem FK para devices — sem uma linha real, o
        // SaveChangesAsync do fluxo de autorização (não deste seed) violaria a constraint.
        var device = Device.Create(tenantId, storeId, "Terminal de teste", DeviceType.Waiter, $"fingerprint-{Guid.NewGuid():N}");
        storeDb.Devices.Add(device);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, (await storeDb.Areas.SingleAsync()).Id, waiterId, device.Id);
    }

    /// <summary>Usuário gerente com PIN real (Argon2, mesmo algoritmo de produção) e a permissão elevável — habilita o fluxo completo de <c>AuthorizeSensitiveActionCommand</c>. Devolve o id gerado pelo agregado (<see cref="AppUser.Create"/> atribui o próprio id).</summary>
    private async Task<Guid> SeedManagerWithPinAsync(World world, string pin)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var hasher = new Argon2CredentialHasher();
        var pinDigester = new HmacPinLookupDigester(Options.Create(new AuthSecretsOptions { PinLookupPepper = TestPinLookupPepper }));

        var role = Role.Create(world.TenantId, "MANAGER", "Gerente de teste", isSystem: true);
        role.UpdatePermissions("[\"order:cancel_started\"]");
        db.Roles.Add(role);

        var manager = AppUser.Create(world.TenantId, "Gerente de teste", email: null, passwordHash: null, pinHash: hasher.Hash(pin), pinLookup: pinDigester.Digest(pin));
        db.Users.Add(manager);
        db.UserRoles.Add(UserRole.Create(world.TenantId, manager.Id, role.Id));

        await db.SaveChangesAsync();

        return manager.Id;
    }

    private async Task<Guid> SeedProductAsync(Guid tenantId, string productName, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, "Único");
        db.ProductVariants.Add(variant);

        db.Prices.Add(Price.Create(tenantId, variant.Id, Channel.DineIn, unitPrice));

        await db.SaveChangesAsync();

        return variant.Id;
    }

    private async Task<Guid> OpenSessionAsync(World world)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"T-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"qr-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }

    /// <summary>Cria um pedido PLACED com um único item já avançado até <paramref name="itemStatus"/> (Queued nunca avança).</summary>
    private async Task<(Guid OrderId, Guid ItemId)> SeedOrderWithSingleItemAsync(
        World world, Guid sessionId, Guid variantId, decimal unitPrice, OrderItemStatus itemStatus)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var order = Order.Create(world.TenantId, world.StoreId, Channel.DineIn, ShortCode(), DateOnly.FromDateTime(DateTime.UtcNow), sessionId: sessionId);
        order.Place();

        var item = OrderItem.Create(world.TenantId, order.Id, variantId, unitPrice);
        AdvanceItemTo(item, itemStatus);
        order.AddItem(item);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return (order.Id, item.Id);
    }

    /// <summary>Cenário "Cancelamento de pedido inteiro": três itens, um já FIRED (iniciado), os outros dois ainda QUEUED.</summary>
    private async Task<(Guid OrderId, IReadOnlyList<Guid> ItemIds)> SeedOrderWithThreeItemsAsync(
        World world, Guid sessionId, Guid variantId, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var order = Order.Create(world.TenantId, world.StoreId, Channel.DineIn, ShortCode(), DateOnly.FromDateTime(DateTime.UtcNow), sessionId: sessionId);
        order.Place();

        var startedItem = OrderItem.Create(world.TenantId, order.Id, variantId, unitPrice);
        startedItem.Fire(world.WaiterId);
        order.AddItem(startedItem);

        var queuedItem1 = OrderItem.Create(world.TenantId, order.Id, variantId, unitPrice);
        order.AddItem(queuedItem1);

        var queuedItem2 = OrderItem.Create(world.TenantId, order.Id, variantId, unitPrice);
        order.AddItem(queuedItem2);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return (order.Id, new[] { startedItem.Id, queuedItem1.Id, queuedItem2.Id });
    }

    /// <summary>Pedido CLOSED (Draft→Placed→InProduction→Ready→Closed) com um item QUEUED — só para exercitar a guarda de estado, o item nunca é o alvo do teste.</summary>
    private async Task<Guid> SeedClosedOrderAsync(World world, Guid sessionId, Guid variantId, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var order = Order.Create(world.TenantId, world.StoreId, Channel.DineIn, ShortCode(), DateOnly.FromDateTime(DateTime.UtcNow), sessionId: sessionId);
        order.Place();
        order.AddItem(OrderItem.Create(world.TenantId, order.Id, variantId, unitPrice));
        order.StartProduction();
        order.MarkReady();
        order.Close();

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order.Id;
    }

    private static void AdvanceItemTo(OrderItem item, OrderItemStatus target)
    {
        if (target == OrderItemStatus.Queued) return;
        item.Fire(Guid.NewGuid());
        if (target == OrderItemStatus.Fired) return;
        item.SendToOven(ovenSlot: null);
        if (target == OrderItemStatus.InOven) return;
        item.TakeOutOfOven();
        if (target == OrderItemStatus.OutOfOven) return;
        item.MarkReady(Guid.NewGuid());
        if (target == OrderItemStatus.Ready) return;
        item.MarkServed(Guid.NewGuid());
    }

    private static string ShortCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static ServiceProvider BuildContainer(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, IOrderConsumptionBroadcaster broadcaster)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton(broadcaster);

        // Auth (ADR-023) — pilha REAL (mesma de produção) para exercitar o fluxo completo de
        // AuthorizeSensitiveActionCommand -> CancelOrderItemCommand/CancelOrderCommand de ponta a
        // ponta, sem duplo nenhum de infraestrutura de segurança.
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
