using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Orders.Commands.AdvanceKdsOrder;

/// <summary>Ver docstring completa de <see cref="AdvanceKdsOrderCommand"/> para a decisão de escopo padrão-vs-lote.</summary>
internal sealed class AdvanceKdsOrderCommandHandler : IRequestHandler<AdvanceKdsOrderCommand, Result<AdvanceKdsOrderResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly IStationBroadcaster _stationBroadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<AdvanceKdsOrderCommandHandler> _logger;

    public AdvanceKdsOrderCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IStationBroadcaster stationBroadcaster,
        ICurrentTenantContext tenantContext,
        ILogger<AdvanceKdsOrderCommandHandler> logger)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _stationBroadcaster = stationBroadcaster;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<AdvanceKdsOrderResponse>> Handle(AdvanceKdsOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result<AdvanceKdsOrderResponse>.Failure("Contexto de loja não identificado.", ApiErrorCodes.TenantContextMissing);
        }

        var normalizedCode = request.ShortCode.Trim();

        var candidates = await _db.OrderItems
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Modifiers)
            .Include(i => i.Fractions)
            .Include(i => i.Order).ThenInclude(o => o.Session)
            .Where(i => i.TenantId == tenantId
                && i.StationId == request.StationId
                && i.Order.ShortCode == normalizedCode
                && i.Status != OrderItemStatus.Served && i.Status != OrderItemStatus.Cancelled)
            .OrderBy(i => i.PlacedAt)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            // Sem distinguir "pedido não existe" de "existe mas não tem item nesta praça" — os
            // dois são o mesmo "não tenho nada pra você aqui" do ponto de vista do operador do
            // teclado, e não vazar a diferença evita um oráculo de enumeração de pedido de outra
            // praça (mesmo espírito do ADR-021 "recurso de outro tenant retorna 404, nunca 403").
            return Result<AdvanceKdsOrderResponse>.Failure(
                "Nenhum pedido com esse código foi encontrado nesta praça.",
                ApiErrorCodes.KdsShortCodeNotFound);
        }

        var targets = request.Batch ? candidates : candidates.Take(1).ToList();

        var actorId = _tenantContext.UserId ?? Guid.Empty;
        var deviceId = _tenantContext.DeviceId;
        var clockResolution = ClockSkewPolicy.Resolve(request.OccurredAt, DateTimeOffset.UtcNow);
        var occurredAt = clockResolution.OccurredAt;

        if (clockResolution.ClockSuspect)
        {
            _logger.LogWarning(
                "Desvio de relógio do dispositivo {DeviceId} acima da tolerância do ADR-034 ao avançar o pedido {ShortCode} pelo KDS: desvio de {DeviationSeconds}s.",
                deviceId,
                normalizedCode,
                clockResolution.Deviation?.TotalSeconds);
        }

        var advanced = new List<OrderItemResponse>();

        foreach (var item in targets)
        {
            if (!OrderItemStatusMachine.TryAdvanceOneStep(item, actorId, occurredAt, deviceId))
            {
                continue; // já em estado final — não impede os demais itens do lote de avançar.
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

            advanced.Add(OrderItemMapper.Map(item, productName));
        }

        if (advanced.Count == 0)
        {
            return Result<AdvanceKdsOrderResponse>.Failure(
                "Todos os itens desse pedido nesta praça já estão em um estado final.",
                ApiErrorCodes.KdsNoEligibleItem);
        }

        return Result<AdvanceKdsOrderResponse>.Success(new AdvanceKdsOrderResponse(advanced));
    }
}
