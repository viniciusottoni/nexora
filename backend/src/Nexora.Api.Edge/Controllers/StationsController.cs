using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Stations.Queries.ListStations;
using Nexora.Contracts.Stations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Leitura de praças de produção no edge (US-017/US-042) — CRUD continua exclusivo da nuvem
/// (<c>Nexora.Api.Cloud.Controllers.StationsController</c>, "cardápio é editado na nuvem"), mas o
/// KDS RODA no edge e precisa listar as praças do próprio tenant offline (ex.: filtro de praça da
/// US-042, "múltiplas praças numa tela" e "todas as praças"/supervisão) — sem isto, a tela de KDS
/// dependeria da nuvem só para saber quais praças existem, contrariando o princípio local-first
/// (ADR-001) que rege exatamente essa tela. Reaproveita a MESMA <see cref="ListStationsQuery"/> do
/// Cloud (Application é agnóstico de qual API está chamando, ADR-039) — os dados já estão
/// sincronizados na base local (ver <c>GetKdsQueueQueryHandler</c>, que já lê <c>Stations</c> e
/// <c>Products</c> localmente).
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/stations")]
public sealed class StationsController : ControllerBase
{
    private readonly ISender _sender;

    public StationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista as praças de produção do tenant autenticado, com a contagem de produtos vinculados.</summary>
    [HttpGet]
    [Authorize(Policy = "StationRead")]
    [ProducesResponseType(typeof(StationListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListStationsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
