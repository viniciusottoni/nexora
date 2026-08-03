using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Areas.Commands.CreateArea;
using Nexora.Application.Areas.Commands.DeleteArea;
using Nexora.Application.Areas.Commands.SetAreaActive;
using Nexora.Application.Areas.Commands.UpdateArea;
using Nexora.Application.Areas.Queries.ListAreas;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de ambientes do salão (US-020 — "cadastrar os ambientes e as mesas do meu salão").
/// Autoridade do dado é a nuvem (US-020, cabeçalho "Aplicações: web-admin, api-cloud"): não existe
/// controller equivalente em <c>Nexora.Api.Edge</c>, o edge só lê a réplica sincronizada.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/areas")]
public sealed class AreasController : ControllerBase
{
    private readonly ISender _sender;

    public AreasController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista os ambientes do tenant autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AreaListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAreasQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cadastra um ambiente (ex.: "Salão", "Varanda").</summary>
    [HttpPost]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAreaRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateAreaCommand(request.Name, request.Position), cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(List), null, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Renomeia/reposiciona um ambiente.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAreaRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateAreaCommand(id, request.Name, request.Position), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Reativa um ambiente desativado.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetAreaActiveCommand(id, true), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Desativa um ambiente sem excluí-lo (permanece no histórico).</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetAreaActiveCommand(id, false), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Exclui (soft delete) um ambiente sem mesas cadastradas.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteAreaCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
