using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.PrepTime.Commands.ReassignProductStation;
using Nexora.Application.Catalog.PrepTime.Commands.UpdateVariantPrepTimeThresholds;
using Nexora.Application.Catalog.PrepTime.Queries.GetVariantPrepTimeAnalysis;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-016 (Tempo de preparo e praça por produto) — tempo de preparo/limiares por variação e
/// praça de produção por produto. Rotas específicas preservam compatibilidade com os clientes
/// operacionais enquanto o CRUD geral permanece em <see cref="ProductsController"/> e
/// <see cref="ProductVariantsController"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog")]
public sealed class ProductPrepTimeController : ControllerBase
{
    private readonly ISender _sender;

    public ProductPrepTimeController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Define tempo de preparo e limiares de atenção/crítico de uma variação (US-016).</summary>
    [HttpPatch("variants/{id:guid}/prep-time")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantPrepTimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrepTime(
        [FromRoute] Guid id,
        [FromBody] UpdatePrepTimeThresholdsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVariantPrepTimeThresholdsCommand(
            id, request.PrepMinutes, request.WarnMinutes, request.CriticalMinutes);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Reatribui (ou remove) a praça de produção de um produto (US-016).</summary>
    [HttpPatch("products/{id:guid}/station")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(ProductStationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignStation(
        [FromRoute] Guid id,
        [FromBody] ReassignStationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReassignProductStationCommand(id, request.StationId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Comparativo entre tempo cadastrado e tempo real (últimos 30 dias) de uma variação (US-016).</summary>
    [HttpGet("variants/{id:guid}/prep-time-analysis")]
    [Authorize(Policy = "ProductRead")]
    [ProducesResponseType(typeof(PrepTimeAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrepTimeAnalysis([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVariantPrepTimeAnalysisQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
