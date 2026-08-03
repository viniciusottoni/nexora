using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Hubs;

/// <summary>
/// Hub SignalR do estado de conectividade edge↔nuvem (US-034 §7/§10, ADR-011) — "todos os
/// dispositivos da loja" (US-034 §7, contrato de <c>sync.status</c>) recebem o aviso de
/// queda/retorno; mesmo padrão de sala por tenant de <c>CatalogAvailabilityHub</c>/
/// <c>TableMapHub</c> (edge é "uma loja = um tenant", ADR-004 — uma sala <c>store:{id}</c> aqui
/// seria idêntica a <c>tenant:{id}</c>, mesmo raciocínio documentado em <c>TableMapHub</c>).
///
/// Hub fino: o cliente só se inscreve (conecta), nunca chama método nele — todo o tráfego é
/// servidor→cliente via <see cref="Microsoft.AspNetCore.SignalR.IHubContext{THub}"/> (ver
/// <c>Realtime.SignalRSyncStatusBroadcaster</c>).
/// </summary>
[Authorize]
public sealed class SyncStatusHub : Hub
{
    public static string TenantGroup(Guid tenantId) => $"tenant:{tenantId}";

    public override async Task OnConnectedAsync()
    {
        var tenantIdClaim = Context.User?.FindFirst("tid")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
        }

        await base.OnConnectedAsync();
    }
}
