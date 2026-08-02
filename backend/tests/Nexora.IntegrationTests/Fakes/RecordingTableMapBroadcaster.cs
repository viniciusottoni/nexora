using System.Collections.Concurrent;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Contracts.Tables;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>Duplo de teste de <see cref="ITableMapBroadcaster"/> — mesmo espírito de <see cref="RecordingOrderConsumptionBroadcaster"/>.</summary>
public sealed class RecordingTableMapBroadcaster : ITableMapBroadcaster
{
    public sealed record SignalCall(Guid TenantId, string Type, object Data);

    public ConcurrentQueue<TableMapEntryResponse> TableChangedCalls { get; } = new();

    public ConcurrentQueue<SignalCall> SignalCalls { get; } = new();

    public Task NotifyTableChangedAsync(Guid tenantId, TableMapEntryResponse table, CancellationToken cancellationToken)
    {
        TableChangedCalls.Enqueue(table);
        return Task.CompletedTask;
    }

    public Task NotifySignalAsync(Guid tenantId, string type, object data, CancellationToken cancellationToken)
    {
        SignalCalls.Enqueue(new SignalCall(tenantId, type, data));
        return Task.CompletedTask;
    }
}
