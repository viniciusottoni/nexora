using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Hubs;

/// <summary>
/// Hub SignalR de alerta dirigido a papel/pessoa (US-025 "Chamar garçom pela mesa", US-026
/// "Solicitar a conta", ADR-011) — diferente de <see cref="TableMapHub"/>/<see cref="CatalogAvailabilityHub"/>
/// (agrupados só por <c>tid</c>, todo o tenant recebe): este hub agrupa CADA conexão por
/// <c>user:{id}</c> (sempre) e por <c>role:{papel}</c> (um grupo por papel do usuário, ex.
/// <c>role:waiter</c>, <c>role:cashier</c>) — a MESMA ideia de "a inscrição é derivada dos claims
/// do token, não solicitada pelo cliente" (ADR-011), só que aqui a claim relevante é
/// <c>ClaimTypes.NameIdentifier</c>/<c>sub</c> (id do usuário) e <c>roles</c> (papéis), não <c>tid</c>.
///
/// Token de sessão de mesa anônimo (esquema <c>TableSession</c>) nunca se conecta aqui — só staff
/// autenticado (garçom, caixa) precisa RECEBER estes alertas; o cliente do salão só os DISPARA via
/// REST (<c>PublicTableController</c>), nunca se inscreve neste hub.
/// </summary>
[Authorize]
public sealed class AlertsHub : Hub
{
    public static string RoleGroup(string role) => $"role:{role.ToLowerInvariant()}";

    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        foreach (var roleClaim in Context.User?.FindAll("roles") ?? Enumerable.Empty<Claim>())
        {
            if (!string.IsNullOrWhiteSpace(roleClaim.Value))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, RoleGroup(roleClaim.Value));
            }
        }

        await base.OnConnectedAsync();
    }
}
