using Nexora.Api.Edge.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Realtime;

/// <summary>
/// Implementação de <see cref="IOrderConsumptionBroadcaster"/> sobre <see cref="IHubContext{THub}"/>
/// (US-024) — réplica do MESMO padrão de <c>SignalRAvailabilityBroadcaster</c> (US-015): sala
/// <c>table:{id}</c> (ADR-011), mensagem <c>{ type, data }</c> pelo método de cliente
/// <c>tableConsumptionChanged</c> (mesmo formato de <c>productAvailabilityChanged</c>).
/// </summary>
internal sealed class SignalRTableConsumptionBroadcaster : IOrderConsumptionBroadcaster
{
    private const string MethodName = "tableConsumptionChanged";

    private readonly IHubContext<TableConsumptionHub> _hub;

    public SignalRTableConsumptionBroadcaster(IHubContext<TableConsumptionHub> hub)
    {
        _hub = hub;
    }

    public Task ItemAdded(
        Guid tenantId, Guid tableId, Guid orderItemId, string productName, Guid? repeatedFromItemId, CancellationToken cancellationToken) =>
        _hub.Clients.Group($"table:{tableId}").SendAsync(
            MethodName,
            new
            {
                type = "order.item.added",
                data = new { orderItemId, productName, repeatedFrom = repeatedFromItemId },
            },
            cancellationToken);

    public Task ItemStatusChanged(
        Guid tenantId, Guid tableId, Guid orderItemId, string productName, string status, CancellationToken cancellationToken) =>
        _hub.Clients.Group($"table:{tableId}").SendAsync(
            MethodName,
            new
            {
                type = $"order.item.{status.ToLowerInvariant()}",
                data = new { orderItemId, productName, status },
            },
            cancellationToken);
}
