using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Orders.Commands.AdvanceKdsItem;

/// <summary>
/// US-041 §7 — mesmo corpo de <c>AdvanceOrderItemStatusCommandHandler</c> (clock skew do ADR-034,
/// evento T1-T4 do EVT-005/006/007/008, broadcast de consumo + de praça), só que o item é
/// carregado por <see cref="AdvanceKdsItemCommand.ItemId"/> isolado (tenant-scoped) — o teclado do
/// KDS não conhece o <c>orderId</c>, só o item que o operador tocou no cartão.
/// </summary>
internal sealed class AdvanceKdsItemCommandHandler : IRequestHandler<AdvanceKdsItemCommand, Result<OrderItemResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly IStationBroadcaster _stationBroadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<AdvanceKdsItemCommandHandler> _logger;

    public AdvanceKdsItemCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IStationBroadcaster stationBroadcaster,
        ICurrentTenantContext tenantContext,
        ILogger<AdvanceKdsItemCommandHandler> logger)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _stationBroadcaster = stationBroadcaster;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<OrderItemResponse>> Handle(AdvanceKdsItemCommand request, CancellationToken cancellationToken)
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

        var actorId = _tenantContext.UserId ?? Guid.Empty;
        var deviceId = _tenantContext.DeviceId;

        var clockResolution = ClockSkewPolicy.Resolve(request.OccurredAt, DateTimeOffset.UtcNow);
        var occurredAt = clockResolution.OccurredAt;

        if (clockResolution.ClockSuspect)
        {
            _logger.LogWarning(
                "Desvio de relógio do dispositivo {DeviceId} acima da tolerância do ADR-034 ao avançar o item {OrderItemId} pelo KDS: desvio de {DeviationSeconds}s.",
                deviceId,
                item.Id,
                clockResolution.Deviation?.TotalSeconds);
        }

        if (!OrderItemStatusMachine.TryAdvanceOneStep(item, actorId, occurredAt, deviceId))
        {
            return Result<OrderItemResponse>.Failure(
                "Este item já está em um estado final.",
                ApiErrorCodes.InvalidStateTransition,
                new Dictionary<string, string[]> { ["current"] = [item.Status.ToString()] });
        }

        _db.DomainEvents.Add(DomainEvent.Create(
            item.TenantId,
            type: OrderItemStatusLabels.ToRealtimeEventType(item.Status),
            aggregateType: "order_item",
            aggregateId: item.Id,
            payload: JsonSerializer.Serialize(new { orderItemId = item.Id, status = OrderItemStatusLabels.ToWireStatus(item.Status) }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: item.Order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            deviceId: deviceId,
            clockSuspect: clockResolution.ClockSuspect));

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
