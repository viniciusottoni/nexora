using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.AddItemToOrder;

/// <summary>Ver docstring de <see cref="AddItemToOrderCommand"/>.</summary>
internal sealed class AddItemToOrderCommandHandler : IRequestHandler<AddItemToOrderCommand, Result<OrderItemResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly ICurrentTenantContext _tenantContext;

    public AddItemToOrderCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        ICurrentTenantContext tenantContext)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OrderItemResponse>> Handle(AddItemToOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Session)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result<OrderItemResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);
        }

        // RN-015/ADR-021: token de sessão de mesa só acrescenta item ao PRÓPRIO pedido — nunca
        // 403, mesma mensagem/código de "pedido inexistente" (mesmo padrão de RepeatOrderItemCommand).
        if (request.RequestingSessionId is { } requestingSessionId && order.SessionId != requestingSessionId)
        {
            return Result<OrderItemResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);
        }

        if (order.Status is not (OrderStatus.Placed or OrderStatus.InProduction))
        {
            return Result<OrderItemResponse>.Failure(
                "Só é possível acrescentar item a um pedido confirmado, ainda em produção.",
                ApiErrorCodes.OrderNotAcceptingItems);
        }

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .SingleOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == order.TenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<OrderItemResponse>.Failure("Variante não encontrada.", ApiErrorCodes.VariantNotFound);
        }

        var product = variant.Product;
        if (!product.IsActive || !product.IsAvailable)
        {
            return Result<OrderItemResponse>.Failure(
                product.UnavailableReason is { Length: > 0 } reason
                    ? $"Este produto está indisponível: {reason}."
                    : "Este produto está indisponível no momento.",
                ApiErrorCodes.ProductUnavailable,
                new Dictionary<string, string[]> { ["variantId"] = new[] { variant.Id.ToString() } });
        }

        var groupSpecs = await _db.ProductModifierGroups
            .Where(pmg => pmg.ProductId == product.Id && pmg.TenantId == order.TenantId)
            .Select(pmg => new ModifierGroupValidator.GroupSpec(
                pmg.Group.Id,
                pmg.Group.Name,
                pmg.Group.MinSelect,
                pmg.Group.MaxSelect,
                pmg.Group.IsRequired,
                pmg.Group.Modifiers.Where(m => m.DeletedAt == null).Select(m => m.Id).ToList()))
            .ToListAsync(cancellationToken);

        var selectedModifierIds = (request.Modifiers ?? Array.Empty<AddOrderItemModifierInput>())
            .Select(m => m.ModifierId).ToList();

        var violation = ModifierGroupValidator.ValidateAll(groupSpecs, selectedModifierIds);
        if (violation is not null)
        {
            return Result<OrderItemResponse>.Failure(
                "Escolha pendente em um grupo de modificadores.",
                violation.Code,
                new Dictionary<string, string[]>
                {
                    ["groupId"] = new[] { violation.GroupId.ToString() },
                    ["groupName"] = new[] { violation.GroupName },
                });
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == order.TenantId, cancellationToken);

        // US-013/US-030: item com fração usa o preço calculado pela regra do tenant, nunca o
        // preço da variante "molde" enviada em variantId — ver OrderItemFractionPricing.
        var pricing = await OrderItemFractionPricing.ResolveAsync(
            _db, order.TenantId, order.Channel, variant.Id, request.Fractions, tenantConfig?.Operation, cancellationToken);
        if (pricing.IsFailure)
        {
            return Result<OrderItemResponse>.Failure(pricing.Error!, pricing.Code, pricing.Errors);
        }

        var edgeNow = DateTimeOffset.UtcNow;
        var clockResolution = ClockSkewPolicy.Resolve(request.OccurredAt, edgeNow);
        var occurredAt = clockResolution.OccurredAt;

        var quantity = request.Quantity < 1 ? (short)1 : request.Quantity;
        var item = OrderItem.Create(
            order.TenantId,
            order.Id,
            variant.Id,
            pricing.Value!.UnitPrice,
            quantity,
            stationId: product.StationId,
            notes: request.Notes,
            deviceId: _tenantContext.DeviceId,
            occurredAt: occurredAt);

        foreach (var modifierInput in request.Modifiers ?? Array.Empty<AddOrderItemModifierInput>())
        {
            var modifier = await _db.Modifiers
                .SingleOrDefaultAsync(m => m.Id == modifierInput.ModifierId && m.TenantId == order.TenantId && m.DeletedAt == null, cancellationToken);
            if (modifier is null)
            {
                return Result<OrderItemResponse>.Failure("Modificador não encontrado.", ApiErrorCodes.ModifierNotFound);
            }

            item.AddModifier(OrderItemModifier.Create(
                order.TenantId, item.Id, modifier.Id, modifier.Name, modifier.PriceDelta, modifierInput.Quantity < 1 ? (short)1 : modifierInput.Quantity));
        }

        foreach (var fraction in pricing.Value.Fractions)
        {
            item.AddFraction(OrderItemFraction.Create(order.TenantId, item.Id, fraction.Variant.Id, fraction.Weight, fraction.UnitPrice));
        }

        order.AddItem(item);

        _db.AuditLogs.Add(AuditLog.Create(
            order.TenantId,
            action: "ORDER_ITEM_ADDED",
            entity: "order_item",
            occurredAt: occurredAt,
            storeId: order.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: item.Id,
            after: JsonSerializer.Serialize(new { orderId = order.Id, variantId = variant.Id, quantity = item.Quantity })));

        // EVT-003 order.item.added (US-030 §6/§7, cenário "Acréscimo a pedido já confirmado").
        _db.DomainEvents.Add(DomainEvent.Create(
            order.TenantId,
            type: "order.item.added",
            aggregateType: "order_item",
            aggregateId: item.Id,
            payload: JsonSerializer.Serialize(new
            {
                variantId = item.VariantId,
                qty = item.Quantity,
                modifiers = item.Modifiers.Select(m => new { m.ModifierId, m.Quantity }),
                fractions = item.Fractions.Select(f => new { f.VariantId, f.Weight }),
                repeatedFrom = (Guid?)null,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: order.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            clockSuspect: clockResolution.ClockSuspect));

        var productName = $"{product.Name} {variant.Name}".Trim();

        if (order.Session is not null)
        {
            await _broadcaster.ItemAdded(order.TenantId, order.Session.TableId, item.Id, productName, repeatedFromItemId: null, cancellationToken);
        }

        return Result<OrderItemResponse>.Success(OrderItemMapper.Map(item, productName));
    }
}
