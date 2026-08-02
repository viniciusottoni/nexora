using Nexora.Api.Edge.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Contracts.Tables;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Realtime;

/// <summary>
/// Implementação real de <see cref="ITableMapBroadcaster"/> sobre <see cref="TableMapHub"/> — só
/// pode viver aqui (Api.Edge), nunca em Infrastructure (ADR-039 proíbe SignalR/ASP.NET Core fora
/// das Apis). Espelha o par porta/adaptador de <c>IAvailabilityBroadcaster</c>/
/// <c>SignalRAvailabilityBroadcaster</c> do módulo de catálogo.
/// </summary>
public sealed class SignalRTableMapBroadcaster : ITableMapBroadcaster
{
    private readonly IHubContext<TableMapHub> _hubContext;

    public SignalRTableMapBroadcaster(IHubContext<TableMapHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyTableChangedAsync(Guid tenantId, TableMapEntryResponse table, CancellationToken cancellationToken) =>
        _hubContext.Clients
            .Group(TableMapHub.TenantGroup(tenantId))
            .SendAsync("table.changed", table, cancellationToken);

    public Task NotifySignalAsync(Guid tenantId, string type, object data, CancellationToken cancellationToken) =>
        _hubContext.Clients
            .Group(TableMapHub.TenantGroup(tenantId))
            .SendAsync(type, data, cancellationToken);
}
