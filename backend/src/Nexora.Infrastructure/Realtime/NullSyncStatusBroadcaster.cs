using Nexora.Application.Abstractions.Realtime;

namespace Nexora.Infrastructure.Realtime;

/// <summary>
/// Implementação no-op de <see cref="ISyncStatusBroadcaster"/> para hosts sem hub SignalR de
/// conectividade edge↔nuvem (Nexora.Api.Cloud não detecta a própria queda de internet — é a nuvem,
/// não tem "internet para a nuvem" para perder; só o edge's <c>SyncOutboxWorker</c> despacha
/// <c>PollSyncHealthCommand</c>, ADR-039 exige que o handler ainda assim resolva a porta neste
/// host). Mesmo padrão de <see cref="NullStationBroadcaster"/>/<c>NullSyncHealthPoller</c>.
/// </summary>
public sealed class NullSyncStatusBroadcaster : ISyncStatusBroadcaster
{
    public Task SyncStatusChanged(
        Guid tenantId, bool online, int pendingEvents, DateTimeOffset? lastSyncAt, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
