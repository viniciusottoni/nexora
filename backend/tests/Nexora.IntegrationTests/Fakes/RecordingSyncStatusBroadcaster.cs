using System.Collections.Concurrent;
using Nexora.Application.Abstractions.Realtime;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="ISyncStatusBroadcaster"/> (US-034 §7) — grava cada chamada em vez
/// de falar com um Hub SignalR real, provando que <c>PollSyncHealthCommandHandler</c> propaga
/// <c>sync.status</c> de forma SÍNCRONA, dentro do próprio <c>Handle</c>, exatamente quando (e só
/// quando) detecta uma transição real de conectividade — mesmo espírito de
/// <see cref="RecordingAvailabilityBroadcaster"/>/<see cref="RecordingStationBroadcaster"/>.
/// </summary>
public sealed class RecordingSyncStatusBroadcaster : ISyncStatusBroadcaster
{
    public sealed record Call(Guid TenantId, bool Online, int PendingEvents, DateTimeOffset? LastSyncAt);

    public ConcurrentQueue<Call> Calls { get; } = new();

    public Task SyncStatusChanged(
        Guid tenantId, bool online, int pendingEvents, DateTimeOffset? lastSyncAt, CancellationToken cancellationToken)
    {
        Calls.Enqueue(new Call(tenantId, online, pendingEvents, lastSyncAt));
        return Task.CompletedTask;
    }
}
