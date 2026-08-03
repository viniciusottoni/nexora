using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;

/// <summary>
/// Avança um <see cref="OrderItem"/> um passo na fila de produção (Queued→Fired→InOven→OutOfOven→
/// Ready→Served), gravando o evento correspondente e propagando via
/// <see cref="IOrderConsumptionBroadcaster"/> — a peça mínima que falta para provar, de ponta a
/// ponta, o requisito de tempo real da US-024 (cenário Gherkin "Atualização automática": "Quando a
/// cozinha marcar um item como pronto... o status deve mudar na tela em até 2 segundos").
///
/// [DECISÃO DE ESCOPO] Isto NÃO é o KDS (US-036 e vizinhas, fora de E-02): não há fila por praça,
/// não há <c>oven_slot</c>/<c>priority_score</c>, não há tela de cozinha. É só o gatilho mínimo,
/// reaproveitando os métodos de domínio já prontos (<see cref="OrderItem.Fire"/>/
/// <see cref="OrderItem.SendToOven"/>/<see cref="OrderItem.TakeOutOfOven"/>/
/// <see cref="OrderItem.MarkReady"/>/<see cref="OrderItem.MarkServed"/>), para que esta wave tenha
/// como demonstrar e testar a entrega em tempo real sem esperar pelo épico de KDS.
/// </summary>
internal sealed class AdvanceOrderItemStatusCommandHandler : IRequestHandler<AdvanceOrderItemStatusCommand, Result<OrderItemResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;

    public AdvanceOrderItemStatusCommandHandler(IApplicationDbContext db, IEventOriginProvider eventOrigin, IOrderConsumptionBroadcaster broadcaster)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
    }

    public async Task<Result<OrderItemResponse>> Handle(AdvanceOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.OrderItems
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Modifiers)
            .Include(i => i.Fractions)
            .Include(i => i.Order).ThenInclude(o => o.Session)
            .SingleOrDefaultAsync(i => i.Id == request.ItemId && i.OrderId == request.OrderId, cancellationToken);

        if (item is null)
        {
            return Result<OrderItemResponse>.Failure("Item não encontrado.", ApiErrorCodes.OrderItemNotFound);
        }

        var actorId = Guid.Empty; // sem tela de KDS nesta wave — ver docstring da classe.
        switch (item.Status)
        {
            case OrderItemStatus.Queued:
                item.Fire(actorId);
                break;
            case OrderItemStatus.Fired:
                item.SendToOven(ovenSlot: null);
                break;
            case OrderItemStatus.InOven:
                item.TakeOutOfOven();
                break;
            case OrderItemStatus.OutOfOven:
                item.MarkReady(actorId);
                break;
            case OrderItemStatus.Ready:
                item.MarkServed(actorId);
                break;
            default:
                return Result<OrderItemResponse>.Failure("Este item já está em um estado final.", ApiErrorCodes.ValidationError);
        }

        var now = DateTimeOffset.UtcNow;
        _db.DomainEvents.Add(DomainEvent.Create(
            item.TenantId,
            type: OrderItemStatusLabels.ToRealtimeEventType(item.Status),
            aggregateType: "order_item",
            aggregateId: item.Id,
            payload: JsonSerializer.Serialize(new { orderItemId = item.Id, status = OrderItemStatusLabels.ToWireStatus(item.Status) }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: item.Order.StoreId));

        var productName = $"{item.Variant.Product.Name} {item.Variant.Name}".Trim();

        if (item.Order.Session is not null)
        {
            await _broadcaster.ItemStatusChanged(
                item.TenantId, item.Order.Session.TableId, item.Id, productName, OrderItemStatusLabels.ToWireStatus(item.Status), cancellationToken);
        }

        return Result<OrderItemResponse>.Success(OrderItemMapper.Map(item, productName));
    }
}
