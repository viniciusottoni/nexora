using System.Collections.Concurrent;
using Nexora.Application.Abstractions.Realtime;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="IOrderConsumptionBroadcaster"/> — mesmo espírito de
/// <see cref="RecordingAvailabilityBroadcaster"/> (US-015): grava cada chamada em vez de falar com
/// um Hub SignalR real, provando que <c>AddOrderItemCommandHandler</c>/
/// <c>RepeatOrderItemCommandHandler</c>/<c>AdvanceOrderItemStatusCommandHandler</c> chamam o
/// broadcaster de forma SÍNCRONA, dentro do próprio <c>Handle</c> (US-024).
/// </summary>
public sealed class RecordingOrderConsumptionBroadcaster : IOrderConsumptionBroadcaster
{
    public sealed record ItemAddedCall(Guid TenantId, Guid TableId, Guid OrderItemId, string ProductName, Guid? RepeatedFromItemId);

    public sealed record ItemStatusChangedCall(Guid TenantId, Guid TableId, Guid OrderItemId, string ProductName, string Status);

    public ConcurrentQueue<ItemAddedCall> ItemAddedCalls { get; } = new();

    public ConcurrentQueue<ItemStatusChangedCall> ItemStatusChangedCalls { get; } = new();

    public Task ItemAdded(Guid tenantId, Guid tableId, Guid orderItemId, string productName, Guid? repeatedFromItemId, CancellationToken cancellationToken)
    {
        ItemAddedCalls.Enqueue(new ItemAddedCall(tenantId, tableId, orderItemId, productName, repeatedFromItemId));
        return Task.CompletedTask;
    }

    public Task ItemStatusChanged(Guid tenantId, Guid tableId, Guid orderItemId, string productName, string status, CancellationToken cancellationToken)
    {
        ItemStatusChangedCalls.Enqueue(new ItemStatusChangedCall(tenantId, tableId, orderItemId, productName, status));
        return Task.CompletedTask;
    }
}
