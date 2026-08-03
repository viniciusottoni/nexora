using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Hubs;

/// <summary>
/// WebSocket local do mapa de mesas (US-023 §10, ADR-011) — mesmo padrão de salas de
/// <c>CatalogAvailabilityHub</c> (grupo por tenant, US-espelho de disponibilidade de catálogo):
/// a inscrição é derivada das claims do token no <c>OnConnectedAsync</c>, nunca escolhida pelo
/// cliente (ADR-011, "a inscrição é derivada dos claims do token, não solicitada pelo cliente").
/// </summary>
/// <remarks>
/// Só entra no grupo de tenant — ADR-011 também prevê <c>store:{id}</c>/<c>role:{papel}</c>/
/// <c>user:{id}</c>, mas o edge é "uma loja = um tenant" (ADR-004): toda conexão deste servidor já
/// pertence à mesma loja, então uma sala <c>store:{id}</c> aqui seria idêntica a <c>tenant:{id}</c>
/// — redundante até o dia em que o edge atender mais de uma loja (fora do escopo atual).
/// </remarks>
[Authorize]
public sealed class TableMapHub : Hub
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

    /// <summary>
    /// Recuperação de mensagens perdidas na reconexão (ADR-011, "Reconexão com recuperação").
    /// US-023 não implementa fila de eventos perdidos (isso pertence ao mesmo mecanismo genérico
    /// que o KDS vai usar — ADR-011 já descreve <c>KdsHub.Resume</c> como o modelo); aqui o cliente
    /// que reconectar simplesmente faz um novo <c>GET /v1/tables</c> (idempotente, sempre
    /// consistente) em vez de reproduzir eventos — o mapa de mesas é POR NATUREZA um snapshot
    /// completo e barato de recarregar, diferente da fila de produção do KDS. Método existe para
    /// o cliente ter um alvo de invocação estável (mesmo formato de chamada do ADR-011) mesmo que
    /// hoje ele só devolva um no-op.
    /// </summary>
    public Task Resume(string? lastEventId) => Task.CompletedTask;
}
