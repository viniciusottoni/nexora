using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Cloud.Hubs;

/// <summary>
/// Hub SignalR (US-015 §"SignalR para realtime local", CLAUDE.md) — propaga
/// <c>product.unavailable</c>/<c>product.available</c> (EVT-051) a mesa, garçom, delivery e caixa
/// em até 2 s (US-015 §2/§4). Hub fino: o cliente só se inscreve (conecta), nunca chama método
/// nele — todo o tráfego é servidor→cliente via <see cref="Microsoft.AspNetCore.SignalR.IHubContext{THub}"/>
/// (ver <c>Realtime.SignalRAvailabilityBroadcaster</c>).
///
/// <see cref="OnConnectedAsync"/> adiciona a conexão a um grupo por tenant (claim JWT <c>tid</c>,
/// mesma claim lida por <see cref="Infrastructure.Auth.CloudCurrentTenantContext"/>) — a nuvem é
/// multi-tenant, então sem agrupamento um evento de um tenant vazaria para clientes de outro
/// (violaria a regra "recurso de outro tenant nunca vaza", ADR-021/ADR-004).
///
/// Réplica idêntica de <c>Nexora.Api.Edge.Hubs.CatalogAvailabilityHub</c> — mesma interface nos
/// dois processos (a marcação de indisponibilidade é bidirecional: cozinha marca no edge, gestor
/// marca na nuvem). [PENDÊNCIA] não existe ainda sincronização de eventos em tempo real entre edge
/// e nuvem — um broadcast neste hub só alcança quem está conectado DIRETAMENTE a este processo
/// (ver docstring de <c>IAvailabilityBroadcaster</c>).
/// </summary>
[Authorize]
public sealed class CatalogAvailabilityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tid")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        }

        await base.OnConnectedAsync();
    }
}
