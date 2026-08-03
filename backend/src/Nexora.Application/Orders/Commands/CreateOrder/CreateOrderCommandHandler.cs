using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Availability;
using Nexora.Application.Catalog.Variants;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.CreateOrder;

/// <summary>
/// US-030 — ver docstring de <see cref="CreateOrderCommand"/>. Duas fases deliberadamente
/// separadas: (1) valida e resolve TODOS os itens (produto disponível, grupo de modificador,
/// preço vigente no canal) sem criar nenhum objeto de domínio nem tocar <c>SaveChangesAsync</c>;
/// (2) só depois de todos os itens passarem, constrói <see cref="Order"/>/<see cref="OrderItem"/> e
/// persiste — garante o critério de aceite "falha parcial não cria pedido incompleto" (nenhum
/// <c>Order</c>/<c>OrderItem</c> é sequer instanciado até a fase 1 inteira ter sucesso).
/// </summary>
internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly IStationBroadcaster _stationBroadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IOrderShortCodeAllocator _shortCodeAllocator;

    public CreateOrderCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IStationBroadcaster stationBroadcaster,
        ICurrentTenantContext tenantContext,
        IOrderShortCodeAllocator shortCodeAllocator)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _stationBroadcaster = stationBroadcaster;
        _tenantContext = tenantContext;
        _shortCodeAllocator = shortCodeAllocator;
    }

    public async Task<Result<CreateOrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result<CreateOrderResponse>.Failure("Contexto de loja não identificado.", ApiErrorCodes.TenantContextMissing);
        }

        if (!ChannelParser.TryParse(request.Channel, out var channel))
        {
            return Result<CreateOrderResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.ValidationError);
        }

        TableSession? session = null;
        if (request.SessionId is { } sessionId)
        {
            session = await _db.TableSessions
                .Include(s => s.Table)
                .SingleOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, cancellationToken);

            if (session is null)
            {
                return Result<CreateOrderResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
            }

            if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
            {
                return Result<CreateOrderResponse>.Failure("Só é possível criar pedido em uma comanda aberta.", ApiErrorCodes.TableSessionNotOpen);
            }
        }

        var storeId = session?.StoreId ?? _tenantContext.StoreId;
        if (storeId is null)
        {
            return Result<CreateOrderResponse>.Failure("Loja não identificada.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

        // Fase 1 — valida e resolve TODOS os itens antes de criar qualquer coisa.
        var resolution = await ResolveItemsAsync(request.Items, tenantId.Value, channel, tenantConfig?.Operation, cancellationToken);
        if (resolution.Failure is not null)
        {
            return resolution.Failure;
        }

        var resolvedItems = resolution.Items!;

        // Fase 2 — constrói e persiste.
        var edgeNow = DateTimeOffset.UtcNow;
        var clockResolution = ClockSkewPolicy.Resolve(request.OccurredAt, edgeNow);
        var placedAt = clockResolution.OccurredAt;

        var startHourUtc = BusinessDayPolicy.ResolveStartHourUtc(tenantConfig?.Operation);
        var businessDay = DateOnly.FromDateTime(BusinessDayPolicy.CurrentBusinessDayStart(placedAt, startHourUtc).UtcDateTime);

        var shortCode = await _shortCodeAllocator.AllocateAsync(storeId.Value, businessDay, cancellationToken);

        var order = Order.Create(
            tenantId.Value,
            storeId.Value,
            channel,
            shortCode,
            businessDay,
            sessionId: session?.Id,
            createdBy: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId);

        var itemResponses = new List<OrderItemResponse>();
        var itemEventPayloads = new List<object>();
        var prepMinutesPerItem = new List<short>();
        var stationItems = new List<StationBroadcastItem>();
        decimal subtotal = 0m;

        foreach (var resolved in resolvedItems)
        {
            var item = OrderItem.Create(
                tenantId.Value,
                order.Id,
                resolved.Variant.Id,
                resolved.UnitPrice,
                resolved.Quantity,
                stationId: resolved.Product.StationId,
                notes: resolved.Notes,
                deviceId: _tenantContext.DeviceId,
                occurredAt: placedAt);

            foreach (var (modifier, quantity) in resolved.Modifiers)
            {
                item.AddModifier(OrderItemModifier.Create(tenantId.Value, item.Id, modifier.Id, modifier.Name, modifier.PriceDelta, quantity));
            }

            foreach (var fraction in resolved.Fractions)
            {
                item.AddFraction(OrderItemFraction.Create(tenantId.Value, item.Id, fraction.Variant.Id, fraction.Weight, fraction.UnitPrice));
            }

            order.AddItem(item);
            subtotal += item.TotalPrice;
            prepMinutesPerItem.Add(resolved.Variant.PrepMinutes);

            var productName = $"{resolved.Product.Name} {resolved.Variant.Name}".Trim();
            itemResponses.Add(OrderItemMapper.Map(item, productName));
            itemEventPayloads.Add(new
            {
                itemId = item.Id,
                variantId = item.VariantId,
                qty = item.Quantity,
                unitPrice = item.UnitPrice,
                totalPrice = item.TotalPrice,
                modifiers = item.Modifiers.Select(m => new { m.ModifierId, m.Quantity }),
                fractions = item.Fractions.Select(f => new { f.VariantId, f.Weight }),
            });

            // US-031 (Roteamento simultâneo para cozinha e caixa) — EVT-004 order.item.queued, um
            // evento POR ITEM (diferente do agregado order.placed abaixo), preenchendo a lacuna do
            // catálogo (doc. 04 §5: "order.placed -> item em QUEUED") que faltava desde US-030.
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId.Value,
                type: "order.item.queued",
                aggregateType: "order_item",
                aggregateId: item.Id,
                payload: JsonSerializer.Serialize(new { orderId = order.Id, stationId = item.StationId, productName, qty = item.Quantity }),
                origin: _eventOrigin.Origin,
                occurredAt: placedAt,
                storeId: storeId.Value,
                actorId: _tenantContext.UserId,
                deviceId: _tenantContext.DeviceId,
                clockSuspect: clockResolution.ClockSuspect));

            stationItems.Add(new StationBroadcastItem(
                item.Id,
                productName,
                item.StationId,
                item.Quantity,
                item.Modifiers.Select(m => m.NameSnapshot).ToArray(),
                item.Notes));
        }

        order.Place(placedAt);
        order.UpdateTotals(subtotal, discountAmount: 0m, deliveryFee: 0m, serviceFeeAmount: 0m, total: subtotal);

        var estimate = OrderPromiseCalculator.Calculate(placedAt, prepMinutesPerItem);
        order.SetPromisedAt(estimate.PromisedAt);

        _db.Orders.Add(order);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId.Value,
            action: "ORDER_CREATED",
            entity: "order",
            occurredAt: placedAt,
            storeId: storeId.Value,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: order.Id,
            after: JsonSerializer.Serialize(new
            {
                channel = channel.ToString(),
                sessionId = order.SessionId,
                itemCount = order.Items.Count,
                total = order.Total,
            })));

        // EVT-001 order.created (rascunho aberto) — ADR-006: nenhuma transição de estado sem
        // evento, mesmo que a transição Draft->Placed aconteça no mesmo comando logo em seguida.
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId.Value,
            type: "order.created",
            aggregateType: "order",
            aggregateId: order.Id,
            payload: JsonSerializer.Serialize(new { channel = channel.ToString(), sessionId = order.SessionId, tableId = session?.TableId }),
            origin: _eventOrigin.Origin,
            occurredAt: placedAt,
            storeId: storeId.Value,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            clockSuspect: clockResolution.ClockSuspect));

        // EVT-002 order.placed (T0) — payload traz items[]/total/promisedAt (US-030 §6).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId.Value,
            type: "order.placed",
            aggregateType: "order",
            aggregateId: order.Id,
            payload: JsonSerializer.Serialize(new
            {
                items = itemEventPayloads,
                total = order.Total,
                promisedAt = estimate.PromisedAt,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: placedAt,
            storeId: storeId.Value,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            clockSuspect: clockResolution.ClockSuspect));

        if (session is not null)
        {
            foreach (var (item, response) in order.Items.Zip(itemResponses))
            {
                await _broadcaster.ItemAdded(tenantId.Value, session.TableId, item.Id, response.Name, repeatedFromItemId: null, cancellationToken);
            }
        }

        // US-031 (Roteamento simultâneo para cozinha e caixa) — roda para TODO canal, com ou sem
        // mesa (cenário Gherkin "Pedido de delivery": "deve chegar ao KDS normalmente"), diferente
        // do broadcast de consumo acima (table:{id}, só existe quando há sessão de mesa).
        await _stationBroadcaster.OrderPlaced(
            tenantId.Value,
            order.Id,
            order.ShortCode,
            session?.TableId,
            session?.Table.Label,
            channel.ToString(),
            stationItems,
            placedAt,
            cancellationToken);

        var orderResponse = new OrderResponse(
            order.Id,
            order.ShortCode,
            OrderStatusLabels.ToWireStatus(order.Status),
            order.SessionId,
            channel.ToString(),
            order.Total,
            order.PlacedAt,
            itemResponses);

        return Result<CreateOrderResponse>.Success(new CreateOrderResponse(orderResponse, estimate.PromisedAt, estimate.EstimatedMinutes));
    }

    private sealed record ResolvedItem(
        ProductVariant Variant,
        Product Product,
        decimal UnitPrice,
        short Quantity,
        string? Notes,
        IReadOnlyList<(Modifier Modifier, short Quantity)> Modifiers,
        IReadOnlyList<OrderItemFractionPricing.ResolvedFraction> Fractions);

    private sealed record ResolutionOutcome(IReadOnlyList<ResolvedItem>? Items, Result<CreateOrderResponse>? Failure);

    private async Task<ResolutionOutcome> ResolveItemsAsync(
        IReadOnlyList<CreateOrderItemInput> items, Guid tenantId, Channel channel, string? tenantOperationJson, CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedItem>();

        for (var index = 0; index < items.Count; index++)
        {
            var input = items[index];

            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .SingleOrDefaultAsync(v => v.Id == input.VariantId && v.TenantId == tenantId && v.DeletedAt == null, cancellationToken);

            if (variant is null)
            {
                return Fail("Variante não encontrada.", ApiErrorCodes.VariantNotFound,
                    new Dictionary<string, string[]> { ["itemIndex"] = new[] { index.ToString() } });
            }

            var product = variant.Product;
            if (!product.IsActive || !product.IsAvailable)
            {
                return Fail(
                    product.UnavailableReason is { Length: > 0 } reason
                        ? $"Este produto está indisponível: {reason}."
                        : "Este produto está indisponível no momento.",
                    ApiErrorCodes.ProductUnavailable,
                    new Dictionary<string, string[]>
                    {
                        ["itemIndex"] = new[] { index.ToString() },
                        ["variantId"] = new[] { variant.Id.ToString() },
                    });
            }

            var groupSpecs = await _db.ProductModifierGroups
                .Where(pmg => pmg.ProductId == product.Id && pmg.TenantId == tenantId)
                .Select(pmg => new ModifierGroupValidator.GroupSpec(
                    pmg.Group.Id,
                    pmg.Group.Name,
                    pmg.Group.MinSelect,
                    pmg.Group.MaxSelect,
                    pmg.Group.IsRequired,
                    pmg.Group.Modifiers.Where(m => m.DeletedAt == null).Select(m => m.Id).ToList()))
                .ToListAsync(cancellationToken);

            var selectedModifierIds = (input.Modifiers ?? Array.Empty<AddOrderItemModifierInput>())
                .Select(m => m.ModifierId).ToList();

            var violation = ModifierGroupValidator.ValidateAll(groupSpecs, selectedModifierIds);
            if (violation is not null)
            {
                return Fail(
                    "Escolha pendente em um grupo de modificadores.",
                    violation.Code,
                    new Dictionary<string, string[]>
                    {
                        ["itemIndex"] = new[] { index.ToString() },
                        ["groupId"] = new[] { violation.GroupId.ToString() },
                        ["groupName"] = new[] { violation.GroupName },
                    });
            }

            // US-013/US-030: item com fração usa o preço calculado pela regra do tenant
            // (Highest/Average/Proportional) a partir do preço de CADA fração — nunca o preço da
            // variante "molde" enviada em variantId. Sem fração, é simplesmente o preço da própria
            // variante no canal do pedido.
            var pricing = await OrderItemFractionPricing.ResolveAsync(
                _db, tenantId, channel, variant.Id, input.Fractions, tenantOperationJson, cancellationToken);
            if (pricing.IsFailure)
            {
                var meta = new Dictionary<string, string[]> { ["itemIndex"] = new[] { index.ToString() } };
                if (pricing.Errors is { Count: > 0 })
                {
                    foreach (var (key, value) in pricing.Errors)
                    {
                        meta[key] = value;
                    }
                }

                return Fail(pricing.Error!, pricing.Code!, meta);
            }

            var resolvedModifiers = new List<(Modifier Modifier, short Quantity)>();
            foreach (var modifierInput in input.Modifiers ?? Array.Empty<AddOrderItemModifierInput>())
            {
                var modifier = await _db.Modifiers
                    .SingleOrDefaultAsync(m => m.Id == modifierInput.ModifierId && m.TenantId == tenantId && m.DeletedAt == null, cancellationToken);
                if (modifier is null)
                {
                    return Fail("Modificador não encontrado.", ApiErrorCodes.ModifierNotFound,
                        new Dictionary<string, string[]> { ["itemIndex"] = new[] { index.ToString() } });
                }

                resolvedModifiers.Add((modifier, modifierInput.Quantity < 1 ? (short)1 : modifierInput.Quantity));
            }

            resolved.Add(new ResolvedItem(
                variant, product, pricing.Value!.UnitPrice, input.Quantity < 1 ? (short)1 : input.Quantity, input.Notes,
                resolvedModifiers, pricing.Value.Fractions));
        }

        return new ResolutionOutcome(resolved, null);

        ResolutionOutcome Fail(string message, string code, IReadOnlyDictionary<string, string[]> meta) =>
            new(null, Result<CreateOrderResponse>.Failure(message, code, meta));
    }
}
