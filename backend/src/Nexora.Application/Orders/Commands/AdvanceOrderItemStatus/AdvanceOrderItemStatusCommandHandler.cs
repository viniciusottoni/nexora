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

namespace Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;

/// <summary>
/// Avança um <see cref="OrderItem"/> um passo na fila de produção (Queued→Fired→InOven→OutOfOven→
/// Ready→Served), gravando o evento correspondente e propagando via
/// <see cref="IOrderConsumptionBroadcaster"/> — a peça mínima que falta para provar, de ponta a
/// ponta, o requisito de tempo real da US-024 (cenário Gherkin "Atualização automática": "Quando a
/// cozinha marcar um item como pronto... o status deve mudar na tela em até 2 segundos").
///
/// [DECISÃO DE ESCOPO] Isto NÃO é o KDS (US-036 e vizinhas, fora de E-02): não há fila por praça,
/// não há tela de cozinha. É só o gatilho mínimo, reaproveitando os métodos de domínio já prontos
/// (<see cref="OrderItem.Fire"/>/<see cref="OrderItem.SendToOven"/>/<see cref="OrderItem.TakeOutOfOven"/>/
/// <see cref="OrderItem.MarkReady"/>/<see cref="OrderItem.MarkServed"/>), para que esta wave tenha
/// como demonstrar e testar a entrega em tempo real sem esperar pelo épico de KDS.
///
/// US-032 (Carimbos de tempo T0 a T5): autor e dispositivo agora vêm de
/// <see cref="ICurrentTenantContext"/> (antes fixos em <see cref="Guid.Empty"/> — comentário
/// removido, ver relatório da história) e o horário gravado passa pela correção de desvio de
/// relógio do ADR-034 (<see cref="ClockSkewPolicy"/>) antes de virar carimbo/`DomainEvent.OccurredAt`.
/// </summary>
internal sealed class AdvanceOrderItemStatusCommandHandler : IRequestHandler<AdvanceOrderItemStatusCommand, Result<OrderItemResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly IStationBroadcaster _stationBroadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<AdvanceOrderItemStatusCommandHandler> _logger;

    public AdvanceOrderItemStatusCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IStationBroadcaster stationBroadcaster,
        ICurrentTenantContext tenantContext,
        ILogger<AdvanceOrderItemStatusCommandHandler> logger)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _stationBroadcaster = stationBroadcaster;
        _tenantContext = tenantContext;
        _logger = logger;
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

        var actorId = _tenantContext.UserId ?? Guid.Empty;
        var deviceId = _tenantContext.DeviceId;

        // ADR-034: aceita o horário do dispositivo (X-Occurred-At) quando o desvio contra o
        // relógio do edge é ≤ 2 min; fora disso usa o relógio do edge e marca o evento como
        // suspeito (ClockSuspect) para diagnóstico — nunca descarta o evento.
        var clockResolution = ClockSkewPolicy.Resolve(request.OccurredAt, DateTimeOffset.UtcNow);
        var occurredAt = clockResolution.OccurredAt;

        if (clockResolution.ClockSuspect)
        {
            _logger.LogWarning(
                "Desvio de relógio do dispositivo {DeviceId} acima da tolerância do ADR-034 ao avançar o item {OrderItemId}: desvio de {DeviationSeconds}s — horário do dispositivo descartado, usado o relógio do edge.",
                deviceId,
                item.Id,
                clockResolution.Deviation?.TotalSeconds);
        }

        if (!OrderItemStatusMachine.TryAdvanceOneStep(item, actorId, occurredAt, deviceId))
        {
            return Result<OrderItemResponse>.Failure("Este item já está em um estado final.", ApiErrorCodes.ValidationError);
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

        // US-031 (Roteamento simultâneo para cozinha e caixa) — a PRÓPRIA praça também precisa saber
        // que o item avançou na fila dela (ex.: sumir da fila do forno ao ser disparado); caixa/
        // garçom/mesa já foram avisados acima pelo broadcaster de consumo (US-024), sem duplicar.
        if (item.StationId is { } stationId)
        {
            await _stationBroadcaster.ItemStatusChanged(
                item.TenantId, stationId, item.Id, productName, OrderItemStatusLabels.ToWireStatus(item.Status), cancellationToken);
        }

        return Result<OrderItemResponse>.Success(OrderItemMapper.Map(item, productName));
    }
}
