using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Cashier.Queries.GetOpenSessions;
using Nexora.Contracts.Cashier;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Painel do caixa (US-050) — leitura da mesma tabela <c>table_session</c> que
/// <see cref="TablesController"/>/<see cref="TableSessionsController"/> já expõem, com um DTO
/// próprio (<see cref="OpenSessionsResponse"/>) porque a persona é outra (caixa, P4, não garçom):
/// densidade máxima, prioridade a conta solicitada, busca por mesa/comanda, totalizador do salão.
/// Autoridade do dado é local (US-050 cabeçalho "Autoridade do dado: Local", RN-005) — por isso
/// este controller vive em <c>Nexora.Api.Edge</c>, não em <c>Nexora.Api.Cloud</c>, mesmo raciocínio
/// de <see cref="TableSessionsController"/>. Controller novo (em vez de um método a mais em
/// <see cref="TableSessionsController"/>) para não inflar mais um controller já grande com uma
/// leitura que serve uma tela completamente diferente.
/// </summary>
[ApiController]
[Authorize(Policy = "TableRead")]
[Route("v1/cash")]
public sealed class CashierController : ControllerBase
{
    private readonly ISender _sender;

    public CashierController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Sessões de mesa abertas (US-050 §7) — nunca mesa livre, nunca sessão já liberada.
    /// </summary>
    /// <param name="q">Busca por mesa (rótulo) ou por comanda (<see cref="OpenSessionEntryResponse.OrderCode"/>) — substring, sem diferenciar maiúsculas/minúsculas.</param>
    /// <param name="sortBy">"urgency" (padrão: conta solicitada primeiro, por espera decrescente) ou "table" (número da mesa) — US-050 §10.</param>
    [HttpGet("open-sessions")]
    [ProducesResponseType(typeof(OpenSessionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenSessions(
        [FromQuery(Name = "q")] string? q,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken)
    {
        var sort = string.Equals(sortBy, "table", StringComparison.OrdinalIgnoreCase)
            ? GetOpenSessionsSortBy.Table
            : GetOpenSessionsSortBy.Urgency;

        var result = await _sender.Send(new GetOpenSessionsQuery(q, sort), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
