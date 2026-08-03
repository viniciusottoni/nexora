using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Tables.Queries.GetTableMap;
using Nexora.Contracts.Tables;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Mapa de mesas do salão (US-023) — tela inicial do garçom (P2) e do caixa (P4). Autenticado,
/// exige a permissão <c>table:read</c> (catálogo em <c>Nexora.Domain.Platform.PermissionCatalog</c>),
/// que já cobre os dois papéis por composição de papel (fora do escopo desta controller).
/// </summary>
[ApiController]
[Authorize(Policy = "TableRead")]
[Route("v1/tables")]
public sealed class TablesController : ControllerBase
{
    private readonly ISender _sender;

    public TablesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lista todas as mesas do salão, agrupáveis por <see cref="TableMapEntryResponse.Area"/> no
    /// frontend (US-023 §10), com status/tempo/valor e os indicadores de ação pendente do §7.
    /// </summary>
    /// <param name="mine">"Minhas mesas" (US-023 §4, "Filtro por responsabilidade") — só mesas cuja sessão aberta pertence ao garçom autenticado.</param>
    /// <param name="sortBy">"urgency" (padrão) ou "label" (número da mesa) — US-023 §10.</param>
    [HttpGet]
    [ProducesResponseType(typeof(TableMapResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMap(
        [FromQuery] bool mine,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken)
    {
        var sort = string.Equals(sortBy, "label", StringComparison.OrdinalIgnoreCase)
            ? TableMapSortBy.Label
            : TableMapSortBy.Urgency;

        var result = await _sender.Send(new GetTableMapQuery(mine, sort), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
