using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Hubs;

/// <summary>
/// Hub SignalR do consumo em tempo real da mesa (US-024, ADR-011) — réplica do MESMO padrão de
/// <see cref="CatalogAvailabilityHub"/> (US-015): hub fino, o cliente só se inscreve (conecta),
/// nunca chama método nele; todo o tráfego é servidor→cliente via
/// <see cref="IHubContext{THub}"/> (ver <c>Realtime.SignalRTableConsumptionBroadcaster</c>).
///
/// Diferente de <see cref="CatalogAvailabilityHub"/> (agrupado por <c>tid</c>, esquema padrão de
/// staff), este hub agrupa por <c>tbl</c> — a sala <c>table:{id}</c> exigida pelo contrato de API
/// da US-024 (§7) — e exige o esquema de autenticação <c>TableSession</c> (token anônimo do
/// cliente do salão, ver <c>Program.cs</c>): só quem tem o token de sessão da PRÓPRIA mesa entra
/// no grupo daquela mesa, nunca de outra (RN-015) — não há como um cliente pedir para entrar no
/// grupo de outra mesa, porque o grupo é derivado da claim do token, nunca de um parâmetro que o
/// cliente informa (ADR-011: "a inscrição é derivada dos claims do token, não solicitada pelo
/// cliente").
/// </summary>
[Authorize(AuthenticationSchemes = "TableSession")]
public sealed class TableConsumptionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tableId = Context.User?.FindFirst("tbl")?.Value;
        if (!string.IsNullOrEmpty(tableId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"table:{tableId}");
        }

        await base.OnConnectedAsync();
    }
}
