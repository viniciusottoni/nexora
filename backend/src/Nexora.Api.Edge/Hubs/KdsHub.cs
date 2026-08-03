using System.Security.Claims;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Orders.Queries.GetKdsQueue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Edge.Hubs;

/// <summary>
/// Hub SignalR do roteamento por praça (US-031, ADR-011) — hub fino: o cliente só se inscreve
/// (conecta) e chama <see cref="Resume"/> na reconexão; todo o tráfego normal é servidor→cliente via
/// <see cref="IHubContext{THub}"/> (ver <c>Realtime.SignalRStationBroadcaster</c>).
///
/// Inscrição AUTOMÁTICA derivada dos claims do token (ADR-011: "a inscrição é derivada dos claims do
/// token, não solicitada pelo cliente" — nunca um <c>subscribe</c> vindo do cliente escolhendo sala):
/// <c>station:{id}</c> da claim <c>stn</c> (praça do dispositivo pareado, ver
/// <see cref="Nexora.Application.Abstractions.Security.AccessClaims.StationId"/>), <c>role:{papel}</c>
/// para cada papel do usuário (mesmo esquema de <see cref="AlertsHub"/> — cozinha loga como papel
/// <c>KITCHEN</c>, caixa como <c>CASHIER</c>, garçom como <c>WAITER</c>) e <c>user:{id}</c>.
/// </summary>
[Authorize]
public sealed class KdsHub : Hub
{
    private readonly ISender _sender;
    private readonly ILogger<KdsHub> _logger;

    public KdsHub(ISender sender, ILogger<KdsHub> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var stationId = Context.User?.FindFirst("stn")?.Value;
        if (!string.IsNullOrEmpty(stationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"station:{stationId}");
        }

        foreach (var roleClaim in Context.User?.FindAll("roles") ?? Enumerable.Empty<Claim>())
        {
            if (!string.IsNullOrWhiteSpace(roleClaim.Value))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{roleClaim.Value.ToLowerInvariant()}");
            }
        }

        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Reconexão com recuperação (ADR-011 §"Reconexão com recuperação") — chamado pelo cliente
    /// via <c>connection.invoke('Resume', lastEventId)</c> depois de <c>onreconnected</c>.
    ///
    /// [DECISÃO DE ESCOPO] Em vez de reproduzir o LOG de eventos perdidos um a um (o que exigiria um
    /// índice estável e sem colisão sobre <c>domain_event</c> só para este propósito — nenhum modelo
    /// de replay pré-existe no repositório, ver tarefa), reenvia o SNAPSHOT corrente da fila da(s)
    /// praça(s)/papel(is) do chamador — a MESMA consulta usada pelo fallback de polling
    /// (<see cref="GetKdsQueueQuery"/>, <c>GET /v1/kds/queue</c>). Um snapshot completo é, por
    /// construção, um superconjunto de qualquer intervalo perdido: cenário Gherkin "Reconexão com
    /// recuperação" ("nenhum pedido deve ficar ausente da fila") passa trivialmente porque NADA fica
    /// de fora, nunca por sorte de janela de tempo. O parâmetro <paramref name="lastEventId"/> é
    /// aceito e logado (telemetria de "tempo em modo degradado"/"eventos reenviados na reconexão",
    /// US-031 §11) mas não filtra o resultado — o volume de itens ATIVOS por praça é pequeno
    /// (dezenas, nunca centenas), então reenviar tudo tem custo desprezível e elimina de raiz a
    /// classe de bug "item perdido por corte de página/timestamp empatado". US-034 (detecção
    /// offline), que reaproveita este mecanismo, pode revisitar para um delta real se o volume um
    /// dia justificar.
    /// </summary>
    public async Task Resume(string? lastEventId)
    {
        var stationIdClaim = Context.User?.FindFirst("stn")?.Value;
        if (!Guid.TryParse(stationIdClaim, out var stationId))
        {
            // Sem praça associada ao dispositivo (ex.: caixa, garçom) — não há fila de KDS para
            // reenviar; a sala role:{papel} já recebeu tudo que perdeu via broadcast normal na
            // reconexão adiante, não há um "snapshot de papel" equivalente ao de praça nesta história.
            return;
        }

        _logger.LogInformation(
            "KdsHub.Resume: praça {StationId}, connectionId {ConnectionId}, lastEventId informado pelo cliente: {LastEventId}",
            stationId, Context.ConnectionId, lastEventId ?? "(nenhum)");

        var result = await _sender.Send(new GetKdsQueueQuery(stationId, lastEventId));
        if (!result.IsSuccess)
        {
            return;
        }

        foreach (var item in result.Value!.Items)
        {
            await Clients.Caller.SendAsync(
                "kdsEvent",
                new
                {
                    type = "order.item.queued",
                    data = new
                    {
                        orderItemId = item.OrderItemId,
                        productName = item.ProductName,
                        quantity = item.Quantity,
                        modifiers = item.Modifiers,
                        notes = item.Notes,
                        status = item.Status,
                        table = item.Table,
                        channel = item.Channel,
                        code = item.OrderCode,
                    },
                });
        }
    }
}
