using System.Collections.Concurrent;
using Nexora.Application.Abstractions.Realtime;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="IAlertsBroadcaster"/> — mesmo espírito de
/// <see cref="RecordingOrderConsumptionBroadcaster"/>: grava cada chamada em vez de falar com um Hub
/// SignalR real, provando que os handlers de US-025/US-026 chamam o broadcaster de forma síncrona.
/// </summary>
public sealed class RecordingAlertsBroadcaster : IAlertsBroadcaster
{
    public sealed record WaiterCalledCall(Guid TenantId, Guid TableId, string TableLabel, Guid? WaiterId);

    public sealed record BillRequestedCall(Guid TenantId, Guid TableId, string TableLabel, string SplitMode, short? People, Guid? WaiterId);

    public sealed record BillRequestCancelledCall(Guid TenantId, Guid TableId, string TableLabel);

    public sealed record WaiterCallEscalatedCall(Guid TenantId, Guid TableId, string TableLabel);

    public ConcurrentQueue<WaiterCalledCall> WaiterCalledCalls { get; } = new();

    public ConcurrentQueue<BillRequestedCall> BillRequestedCalls { get; } = new();

    public ConcurrentQueue<BillRequestCancelledCall> BillRequestCancelledCalls { get; } = new();

    public ConcurrentQueue<WaiterCallEscalatedCall> WaiterCallEscalatedCalls { get; } = new();

    public Task WaiterCalled(Guid tenantId, Guid tableId, string tableLabel, Guid? waiterId, CancellationToken cancellationToken)
    {
        WaiterCalledCalls.Enqueue(new WaiterCalledCall(tenantId, tableId, tableLabel, waiterId));
        return Task.CompletedTask;
    }

    public Task BillRequested(
        Guid tenantId, Guid tableId, string tableLabel, string splitMode, short? people, Guid? waiterId, CancellationToken cancellationToken)
    {
        BillRequestedCalls.Enqueue(new BillRequestedCall(tenantId, tableId, tableLabel, splitMode, people, waiterId));
        return Task.CompletedTask;
    }

    public Task BillRequestCancelled(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken)
    {
        BillRequestCancelledCalls.Enqueue(new BillRequestCancelledCall(tenantId, tableId, tableLabel));
        return Task.CompletedTask;
    }

    public Task WaiterCallEscalated(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken)
    {
        WaiterCallEscalatedCalls.Enqueue(new WaiterCallEscalatedCall(tenantId, tableId, tableLabel));
        return Task.CompletedTask;
    }
}
