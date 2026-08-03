using Nexora.Api.Edge.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Realtime;

/// <summary>
/// Implementação de <see cref="ISyncStatusBroadcaster"/> sobre <see cref="IHubContext{THub}"/>
/// (US-034 §7) — réplica do MESMO padrão de <c>SignalRAvailabilityBroadcaster</c>/
/// <c>SignalRTableMapBroadcaster</c>: mensagem <c>{ type, data }</c>, sala <c>tenant:{id}</c>
/// (ADR-011, ADR-004 "uma loja = um tenant" no edge).
/// </summary>
internal sealed class SignalRSyncStatusBroadcaster : ISyncStatusBroadcaster
{
    private const string MethodName = "syncStatus";

    private readonly IHubContext<SyncStatusHub> _hub;

    public SignalRSyncStatusBroadcaster(IHubContext<SyncStatusHub> hub)
    {
        _hub = hub;
    }

    /// <summary>Contrato exato (US-034 §7): <c>{ "type": "sync.status", "data": { "online", "pendingEvents", "lastSyncAt" } }</c>.</summary>
    public Task SyncStatusChanged(
        Guid tenantId, bool online, int pendingEvents, DateTimeOffset? lastSyncAt, CancellationToken cancellationToken) =>
        _hub.Clients.Group(SyncStatusHub.TenantGroup(tenantId)).SendAsync(
            MethodName,
            new
            {
                type = "sync.status",
                data = new { online, pendingEvents, lastSyncAt },
            },
            cancellationToken);
}
