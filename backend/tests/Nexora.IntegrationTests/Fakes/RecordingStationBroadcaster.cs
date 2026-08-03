using System.Collections.Concurrent;
using Nexora.Application.Abstractions.Realtime;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="IStationBroadcaster"/> — mesmo espírito de
/// <see cref="RecordingOrderConsumptionBroadcaster"/> (US-024): grava cada chamada em vez de falar
/// com um Hub SignalR real, provando que <c>CreateOrderCommandHandler</c>/
/// <c>AddOrderItemCommandHandler</c>/<c>AdvanceOrderItemStatusCommandHandler</c> chamam o
/// broadcaster de forma SÍNCRONA, dentro do próprio <c>Handle</c> (US-031).
/// </summary>
public sealed class RecordingStationBroadcaster : IStationBroadcaster
{
    public sealed record OrderPlacedCall(
        Guid TenantId,
        Guid OrderId,
        string ShortCode,
        Guid? TableId,
        string? TableLabel,
        string Channel,
        IReadOnlyList<StationBroadcastItem> Items);

    public sealed record ItemQueuedCall(
        Guid TenantId, Guid OrderId, string ShortCode, Guid? TableId, string? TableLabel, string Channel, StationBroadcastItem Item);

    public sealed record ItemStatusChangedCall(Guid TenantId, Guid StationId, Guid OrderItemId, string ProductName, string Status);

    public ConcurrentQueue<OrderPlacedCall> OrderPlacedCalls { get; } = new();

    public ConcurrentQueue<ItemQueuedCall> ItemQueuedCalls { get; } = new();

    public ConcurrentQueue<ItemStatusChangedCall> ItemStatusChangedCalls { get; } = new();

    public Task OrderPlaced(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        IReadOnlyList<StationBroadcastItem> items,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        OrderPlacedCalls.Enqueue(new OrderPlacedCall(tenantId, orderId, shortCode, tableId, tableLabel, channel, items));
        return Task.CompletedTask;
    }

    public Task ItemQueued(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        StationBroadcastItem item,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        ItemQueuedCalls.Enqueue(new ItemQueuedCall(tenantId, orderId, shortCode, tableId, tableLabel, channel, item));
        return Task.CompletedTask;
    }

    public Task ItemStatusChanged(
        Guid tenantId, Guid stationId, Guid orderItemId, string productName, string status, CancellationToken cancellationToken)
    {
        ItemStatusChangedCalls.Enqueue(new ItemStatusChangedCall(tenantId, stationId, orderItemId, productName, status));
        return Task.CompletedTask;
    }
}
