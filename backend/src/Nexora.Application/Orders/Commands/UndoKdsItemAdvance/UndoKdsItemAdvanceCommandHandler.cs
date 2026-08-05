using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.UndoKdsItemAdvance;

/// <summary>
/// US-041 §3 (cenário "Desfazer avanço acidental") — reverte a última transição de um item, um
/// passo, dentro da janela curta. Grava um evento de CORREÇÃO ao lado do original (RN "a correção
/// deve ser registrada, sem apagar o evento original") — o `domain_event` do avanço desfeito
/// continua na tabela append-only (ADR-006), este handler nunca o remove nem o edita.
/// </summary>
internal sealed class UndoKdsItemAdvanceCommandHandler : IRequestHandler<UndoKdsItemAdvanceCommand, Result<OrderItemResponse>>
{
    /// <summary>US-041 §4: "dentro de 10 segundos".</summary>
    internal static readonly TimeSpan UndoWindow = TimeSpan.FromSeconds(10);

    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly IStationBroadcaster _stationBroadcaster;
    private readonly ICurrentTenantContext _tenantContext;

    public UndoKdsItemAdvanceCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IStationBroadcaster stationBroadcaster,
        ICurrentTenantContext tenantContext)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _stationBroadcaster = stationBroadcaster;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OrderItemResponse>> Handle(UndoKdsItemAdvanceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result<OrderItemResponse>.Failure("Contexto de loja não identificado.", ApiErrorCodes.TenantContextMissing);
        }

        var item = await _db.OrderItems
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Modifiers)
            .Include(i => i.Fractions)
            .Include(i => i.Order).ThenInclude(o => o.Session)
            .SingleOrDefaultAsync(i => i.Id == request.ItemId && i.TenantId == tenantId, cancellationToken);

        if (item is null)
        {
            return Result<OrderItemResponse>.Failure("Item não encontrado.", ApiErrorCodes.OrderItemNotFound);
        }

        var lastTransitionAt = item.LastTransitionAt;
        var now = DateTimeOffset.UtcNow;

        if (lastTransitionAt is null || now - lastTransitionAt.Value > UndoWindow)
        {
            return Result<OrderItemResponse>.Failure(
                "A janela para desfazer este avanço já passou.",
                ApiErrorCodes.KdsUndoWindowExpired);
        }

        var previousStatus = item.Status;

        try
        {
            item.UndoLastTransition();
        }
        catch (DomainException ex)
        {
            return Result<OrderItemResponse>.Failure(ex.Message, ApiErrorCodes.InvalidStateTransition);
        }

        var actorId = _tenantContext.UserId ?? Guid.Empty;
        var deviceId = _tenantContext.DeviceId;

        _db.DomainEvents.Add(DomainEvent.Create(
            item.TenantId,
            type: "order.item.correction",
            aggregateType: "order_item",
            aggregateId: item.Id,
            payload: JsonSerializer.Serialize(new
            {
                orderItemId = item.Id,
                from = OrderItemStatusLabels.ToWireStatus(previousStatus),
                to = OrderItemStatusLabels.ToWireStatus(item.Status),
                reason = "kds_undo",
            }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: item.Order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            deviceId: deviceId,
            clockSuspect: false));

        var productName = $"{item.Variant.Product.Name} {item.Variant.Name}".Trim();

        if (item.Order.Session is not null)
        {
            await _broadcaster.ItemStatusChanged(
                item.TenantId, item.Order.Session.TableId, item.Id, productName, OrderItemStatusLabels.ToWireStatus(item.Status), cancellationToken);
        }

        if (item.StationId is { } stationId)
        {
            await _stationBroadcaster.ItemStatusChanged(
                item.TenantId, stationId, item.Id, productName, OrderItemStatusLabels.ToWireStatus(item.Status), cancellationToken);
        }

        return Result<OrderItemResponse>.Success(OrderItemMapper.Map(item, productName));
    }
}
