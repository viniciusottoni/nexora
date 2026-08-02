using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.RepeatOrderItem;

/// <summary>
/// US-028 (Repetir item com um toque) — cria um novo <see cref="OrderItem"/> idêntico em
/// composição ao original (mesmos modificadores/frações/observações, <see cref="OrderItemCloner"/>),
/// com o preço VIGENTE da variante (nunca o preço gravado no item original — cenário Gherkin "Preço
/// atualizado"), bloqueado com <see cref="ApiErrorCodes.ProductUnavailable"/> se o produto
/// indisponível (cenário "Item indisponível") e preservando <c>stationId</c> do item original
/// (§"segue o roteamento normal à praça" — o roteamento de verdade a uma praça não existe nesta
/// solution ainda, então preservar o mesmo <c>stationId</c> é a aproximação mínima documentada).
/// </summary>
internal sealed class RepeatOrderItemCommandHandler : IRequestHandler<RepeatOrderItemCommand, Result<RepeatOrderItemResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;

    public RepeatOrderItemCommandHandler(IApplicationDbContext db, IEventOriginProvider eventOrigin, IOrderConsumptionBroadcaster broadcaster)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
    }

    public async Task<Result<RepeatOrderItemResponse>> Handle(RepeatOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Session)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result<RepeatOrderItemResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);
        }

        // RN-015/ADR-021: token de sessão de mesa só repete item do PRÓPRIO pedido — nunca 403,
        // a mesma mensagem/código de "pedido inexistente" (ver docstring de RepeatOrderItemCommand).
        if (request.RequestingSessionId is { } requestingSessionId && order.SessionId != requestingSessionId)
        {
            return Result<RepeatOrderItemResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);
        }

        var original = await _db.OrderItems
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Modifiers)
            .Include(i => i.Fractions)
            .SingleOrDefaultAsync(i => i.Id == request.ItemId && i.OrderId == order.Id, cancellationToken);

        if (original is null)
        {
            return Result<RepeatOrderItemResponse>.Failure("Item não encontrado.", ApiErrorCodes.OrderItemNotFound);
        }

        var product = original.Variant.Product;
        if (!product.IsActive || !product.IsAvailable)
        {
            return Result<RepeatOrderItemResponse>.Failure(
                product.UnavailableReason is { Length: > 0 } reason
                    ? $"Não é possível repetir — este produto está indisponível: {reason}."
                    : "Não é possível repetir — este produto está indisponível no momento.",
                ApiErrorCodes.ProductUnavailable);
        }

        var currentPrice = await ResolveCurrentPriceAsync(original.VariantId, order.TenantId, cancellationToken);
        if (currentPrice is null)
        {
            return Result<RepeatOrderItemResponse>.Failure("Este item não tem preço vigente cadastrado.", ApiErrorCodes.OrderItemVariantPriceNotFound);
        }

        var repeated = OrderItem.Create(
            order.TenantId,
            order.Id,
            original.VariantId,
            currentPrice.Value, // preço VIGENTE — nunca original.UnitPrice (cenário "Preço atualizado")
            original.Quantity,
            stationId: original.StationId,
            notes: OrderItemCloner.CopyNotes(original),
            repeatedFromItemId: original.Id);

        foreach (var selection in OrderItemCloner.CopyModifiers(original))
        {
            var modifier = await _db.Modifiers
                .SingleOrDefaultAsync(m => m.Id == selection.ModifierId && m.TenantId == order.TenantId && m.DeletedAt == null, cancellationToken);
            if (modifier is null)
            {
                return Result<RepeatOrderItemResponse>.Failure("Um dos modificadores do item original não existe mais.", ApiErrorCodes.ModifierNotFound);
            }

            repeated.AddModifier(OrderItemModifier.Create(
                order.TenantId, repeated.Id, modifier.Id, modifier.Name, modifier.PriceDelta, selection.Quantity));
        }

        foreach (var selection in OrderItemCloner.CopyFractions(original))
        {
            var fractionVariant = await _db.ProductVariants
                .SingleOrDefaultAsync(v => v.Id == selection.VariantId && v.TenantId == order.TenantId && v.DeletedAt == null, cancellationToken);
            if (fractionVariant is null)
            {
                return Result<RepeatOrderItemResponse>.Failure("Uma das frações do item original não existe mais.", ApiErrorCodes.VariantNotFound);
            }

            var fractionPrice = await ResolveCurrentPriceAsync(fractionVariant.Id, order.TenantId, cancellationToken);
            if (fractionPrice is null)
            {
                return Result<RepeatOrderItemResponse>.Failure("Uma das frações do item original não tem preço vigente.", ApiErrorCodes.OrderItemVariantPriceNotFound);
            }

            repeated.AddFraction(OrderItemFraction.Create(
                order.TenantId, repeated.Id, fractionVariant.Id, selection.Weight, fractionPrice.Value, selection.SortOrder));
        }

        order.AddItem(repeated);

        var now = DateTimeOffset.UtcNow;
        _db.AuditLogs.Add(AuditLog.Create(
            order.TenantId,
            action: "ORDER_ITEM_REPEATED",
            entity: "order_item",
            occurredAt: now,
            storeId: order.StoreId,
            entityId: repeated.Id,
            before: JsonSerializer.Serialize(new { repeatedFromItemId = original.Id, originalUnitPrice = original.UnitPrice }),
            after: JsonSerializer.Serialize(new { orderId = order.Id, variantId = repeated.VariantId, quantity = repeated.Quantity, unitPrice = repeated.UnitPrice })));

        // EVT-003 order.item.added (US-028 §6) — payload exigido: variantId, qty, modifiers, fractions, repeatedFrom.
        _db.DomainEvents.Add(DomainEvent.Create(
            order.TenantId,
            type: "order.item.added",
            aggregateType: "order_item",
            aggregateId: repeated.Id,
            payload: JsonSerializer.Serialize(new
            {
                variantId = repeated.VariantId,
                qty = repeated.Quantity,
                modifiers = repeated.Modifiers.Select(m => new { m.ModifierId, m.Quantity }),
                fractions = repeated.Fractions.Select(f => new { f.VariantId, f.Weight }),
                repeatedFrom = original.Id,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: order.StoreId));

        var productName = $"{product.Name} {original.Variant.Name}".Trim();

        if (order.Session is not null)
        {
            await _broadcaster.ItemAdded(order.TenantId, order.Session.TableId, repeated.Id, productName, repeatedFromItemId: original.Id, cancellationToken);
        }

        return Result<RepeatOrderItemResponse>.Success(new RepeatOrderItemResponse(OrderItemMapper.Map(repeated, productName)));
    }

    private async Task<decimal?> ResolveCurrentPriceAsync(Guid variantId, Guid tenantId, CancellationToken cancellationToken)
    {
        var price = await _db.Prices
            .Where(p => p.VariantId == variantId && p.TenantId == tenantId && p.Channel == Channel.DineIn && p.ValidTo == null)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return price?.Amount;
    }
}
