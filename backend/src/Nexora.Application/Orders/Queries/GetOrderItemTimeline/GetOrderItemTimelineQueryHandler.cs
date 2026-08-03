using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Queries.GetOrderItemTimeline;

/// <summary>
/// US-032 (Carimbos de tempo T0 a T5) §7 — devolve os seis carimbos (cada um com autor e
/// dispositivo, RN-004) e os sete intervalos derivados MET-001 a MET-007
/// (<see cref="OrderItemDurationCalculator"/>). É o endpoint que sustenta o drill-down do painel
/// (RF-BI-11: "do número ao pedido individual em no máximo três toques").
///
/// [DECISÃO DE ESCOPO/DESIGN] <c>OrderItem</c> não tem um campo próprio de autor/dispositivo para
/// T0 (<c>placedAt</c>) — só <see cref="OrderItem.PlacedDeviceId"/> (adicionado nesta história).
/// O autor de T0 é lido de <see cref="Order.CreatedBy"/> (quem lançou o pedido), não um campo novo
/// em <c>OrderItem</c> — o brief desta história não pediu um <c>PlacedBy</c> dedicado; registrado
/// aqui para quem consumir/estender este endpoint depois.
/// </summary>
internal sealed class GetOrderItemTimelineQueryHandler : IRequestHandler<GetOrderItemTimelineQuery, Result<OrderItemTimelineResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetOrderItemTimelineQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<OrderItemTimelineResponse>> Handle(GetOrderItemTimelineQuery request, CancellationToken cancellationToken)
    {
        var item = await _db.OrderItems
            .AsNoTracking()
            .Include(i => i.Order)
            .SingleOrDefaultAsync(i => i.Id == request.ItemId && i.OrderId == request.OrderId, cancellationToken);

        if (item is null)
        {
            return Result<OrderItemTimelineResponse>.Failure("Item não encontrado.", ApiErrorCodes.OrderItemNotFound);
        }

        // ATENÇÃO: List<Guid>, não Guid[] — EF Core 9/.NET 10 traduz mal `array.Contains(x)` dentro
        // de uma expressão de consulta quando o array é capturado por closure (ambiguidade entre
        // `Enumerable.Contains` e os novos overloads de `MemoryExtensions` sobre `ReadOnlySpan<T>`,
        // que o funcletizer do EF Core tenta compilar e falha com
        // `TypeLoadException`/`InvalidOperationException` em tempo de execução). `List<T>.Contains`
        // não tem essa ambiguidade.
        var actorIds = new[] { item.Order.CreatedBy, item.FiredBy, item.OvenInBy, item.OvenOutBy, item.ReadyBy, item.ServedBy }
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var deviceIds = new[] { item.PlacedDeviceId, item.FiredDeviceId, item.OvenInDeviceId, item.OvenOutDeviceId, item.ReadyDeviceId, item.ServedDeviceId }
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var actors = actorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var devices = deviceIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Devices.AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Label })
                .ToDictionaryAsync(d => d.Id, d => d.Label, cancellationToken);

        var timestamps = new OrderItemTimelineTimestampsResponse(
            Timestamp(item.PlacedAt, item.Order.CreatedBy, item.PlacedDeviceId, actors, devices),
            Timestamp(item.FiredAt, item.FiredBy, item.FiredDeviceId, actors, devices),
            Timestamp(item.OvenInAt, item.OvenInBy, item.OvenInDeviceId, actors, devices),
            Timestamp(item.OvenOutAt, item.OvenOutBy, item.OvenOutDeviceId, actors, devices),
            Timestamp(item.ReadyAt, item.ReadyBy, item.ReadyDeviceId, actors, devices),
            Timestamp(item.ServedAt, item.ServedBy, item.ServedDeviceId, actors, devices));

        var durations = OrderItemDurationCalculator.Calculate(
            item.PlacedAt, item.FiredAt, item.OvenInAt, item.OvenOutAt, item.ReadyAt, item.ServedAt);

        var durationsResponse = new OrderItemTimelineDurationsResponse(
            durations.QueueSeconds,
            durations.AssemblySeconds,
            durations.CookSeconds,
            durations.FinishSeconds,
            durations.ServeSeconds,
            durations.PrepSeconds,
            durations.TotalSeconds);

        return Result<OrderItemTimelineResponse>.Success(new OrderItemTimelineResponse(item.Id, timestamps, durationsResponse));
    }

    private static OrderItemTimestampResponse Timestamp(
        DateTimeOffset? at,
        Guid? actorId,
        Guid? deviceId,
        IReadOnlyDictionary<Guid, string> actors,
        IReadOnlyDictionary<Guid, string> devices)
    {
        OrderItemTimelineActorResponse? actor = actorId is { } id && actors.TryGetValue(id, out var name)
            ? new OrderItemTimelineActorResponse(id, name)
            : null;

        OrderItemTimelineDeviceResponse? device = deviceId is { } did && devices.TryGetValue(did, out var label)
            ? new OrderItemTimelineDeviceResponse(did, label)
            : null;

        return new OrderItemTimestampResponse(at, actor, device);
    }
}
