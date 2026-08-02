using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Stations.Commands.CreateStation;
using Nexora.Application.Stations.Commands.DeleteStation;
using Nexora.Application.Stations.Commands.UpdateStation;
using Nexora.Application.Stations.Queries.ListStations;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Stations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de praças de produção (US-017) — cardápio/cozinha são autoridade da nuvem (CLAUDE.md:
/// "Cardápio é editado na nuvem e apenas lido no local"). Exige permissão <c>catalog:read</c>
/// (listar) e <c>catalog:write</c> (criar/editar/excluir), mesmo catálogo de permissões usado por
/// produtos/categorias (Nexora.Domain.Platform.PermissionCatalog).
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

    /// <summary>Cria uma praça de produção na loja do tenant autenticado.</summary>
    [HttpPost]
    [Authorize(Policy = "StationWrite")]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateStationCommand(
            request.Code,
            request.Name,
            request.Color,
            request.CapacitySlots,
            request.IsBottleneck,
            request.Position);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(List), null, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza campos de uma praça de produção — inclusive marcá-la/desmarcá-la como gargalo.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "StationWrite")]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateStationRequest request,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("station.id", id);
        var command = new UpdateStationCommand(id, request.Name, request.Color, request.CapacitySlots, request.IsBottleneck, request.Position);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Exclui (soft delete) uma praça de produção — recusado se houver produtos vinculados (US-017 §4).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "StationWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("station.id", id);
        var result = await _sender.Send(new DeleteStationCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
