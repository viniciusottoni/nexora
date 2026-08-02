using Nexora.Api.Edge.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Realtime;

/// <summary>
/// Implementação de <see cref="IAvailabilityBroadcaster"/> sobre <see cref="IHubContext{THub}"/>
/// (US-015) — réplica do gêmeo de <c>Nexora.Api.Cloud</c>, só pode viver aqui (Api.Edge), nunca em
/// Application/Infrastructure (ADR-039). É este processo que cumpre "funciona integralmente
/// offline dentro da rede local" (US-015 §9) — o edge propaga por WebSocket local sem depender da
/// nuvem estar acessível.
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
