using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Hubs;

/// <summary>
/// Hub SignalR (US-015 §"SignalR para realtime local", CLAUDE.md) — propaga
/// <c>product.unavailable</c>/<c>product.available</c> (EVT-051) a mesa, garçom, delivery e caixa
/// em até 2 s (US-015 §2/§4). Hub fino: o cliente só se inscreve (conecta), nunca chama método
/// nele — todo o tráfego é servidor→cliente via <see cref="Microsoft.AspNetCore.SignalR.IHubContext{THub}"/>
/// (ver <c>Realtime.SignalRAvailabilityBroadcaster</c>).
///
/// No edge, "uma loja = um tenant" (ADR-004, ver <c>EdgeCurrentTenantContext</c>) — todo cliente
/// conectado já pertence ao mesmo tenant, então o agrupamento por <c>tid</c> em
/// <see cref="OnConnectedAsync"/> existe só para manter a MESMA implementação do gêmeo de
/// <c>Nexora.Api.Cloud</c> (onde o agrupamento é obrigatório, multi-tenant) — não é estritamente
/// necessário aqui, mas evita duas implementações divergentes do mesmo hub.
///
/// US-015 §9 ("comportamento offline"): "funciona integralmente offline dentro da rede local" — é
/// justamente este hub local, sem depender da nuvem, que cumpre esse requisito. [PENDÊNCIA] não
/// existe ainda sincronização de eventos em tempo real entre edge e nuvem — ver docstring de
/// <c>IAvailabilityBroadcaster</c>.
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
