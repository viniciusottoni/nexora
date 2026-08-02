using Nexora.Api.Cloud.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Cloud.Realtime;

/// <summary>
/// Implementação de <see cref="IAvailabilityBroadcaster"/> sobre <see cref="IHubContext{THub}"/>
/// (US-015) — só pode viver aqui (Api.Cloud), nunca em Application/Infrastructure, porque depende
/// de <c>Microsoft.AspNetCore.SignalR</c> (ADR-039). Envia ao grupo do tenant (ver
/// <see cref="CatalogAvailabilityHub.OnConnectedAsync"/>) o mesmo formato de mensagem descrito em
/// US-015 §7 (<c>{ type, data }</c>), sob o nome de método <c>"productAvailabilityChanged"</c> que
/// o cliente (web-kds/web-menu/web-pos) escuta.
/// </summary>
internal sealed class SignalRAvailabilityBroadcaster : IAvailabilityBroadcaster
{
    private const string MethodName = "productAvailabilityChanged";

    private readonly IHubContext<CatalogAvailabilityHub> _hub;

    public SignalRAvailabilityBroadcaster(IHubContext<CatalogAvailabilityHub> hub)
    {
        _hub = hub;
    }

    public Task ProductMarkedUnavailableAsync(
        Guid tenantId, Guid productId, string reason, DateTimeOffset unavailableSince, CancellationToken cancellationToken) =>
        _hub.Clients.Group(tenantId.ToString()).SendAsync(
            MethodName,
            new
            {
                type = "product.unavailable",
                data = new { productId, reason, unavailableSince },
            },
            cancellationToken);

    public Task ProductMarkedAvailableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
        _hub.Clients.Group(tenantId.ToString()).SendAsync(
            MethodName,
            new
            {
                type = "product.available",
                data = new { productId },
            },
            cancellationToken);
}
