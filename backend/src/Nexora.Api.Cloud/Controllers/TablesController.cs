using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Tables.Commands.CreateTable;
using Nexora.Application.Tables.Commands.CreateTablesBulk;
using Nexora.Application.Tables.Commands.DeleteTable;
using Nexora.Application.Tables.Commands.RotateTableQrToken;
using Nexora.Application.Tables.Commands.SetTableActive;
using Nexora.Application.Tables.Commands.UpdateTable;
using Nexora.Application.Tables.Queries.ExportTablesQrCodesPdf;
using Nexora.Application.Tables.Queries.ListTables;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de mesas do salão, criação em lote e exportação de QR Codes (US-020). Autoridade do dado
/// é a nuvem: o edge só lê a réplica sincronizada de <c>dining_table</c> (US-020 §9) — não existe
/// controller de escrita equivalente em <c>Nexora.Api.Edge</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/tables")]
public sealed class TablesController : ControllerBase
{
    private readonly ISender _sender;

    public TablesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista as mesas do tenant autenticado, opcionalmente filtradas por ambiente.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(TableListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? areaId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListTablesQuery(areaId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cadastra uma mesa física do salão, com <c>qr_token</c> gerado automaticamente.</summary>
    [HttpPost]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(typeof(TableResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTableRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateTableCommand(request.AreaId, request.Label, request.Seats), cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(List), null, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cenário Gherkin "Criação em lote": cria mesas com rótulos sequenciais de <c>From</c> a
    /// <c>To</c> (ex.: "criar mesas 1 a 20") numa única transação.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(typeof(TablesBulkResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBulk([FromBody] CreateTablesBulkRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTablesBulkCommand(request.AreaId, request.From, request.To, request.Seats);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(List), null, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza rótulo/capacidade/ambiente/ordem de uma mesa.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(typeof(TableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTableRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTableCommand(id, request.AreaId, request.Label, request.Seats, request.SortOrder);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cenário Gherkin "Rotação de token": gera um novo <c>qr_token</c> — o anterior deixa de
    /// funcionar imediatamente. O gestor precisa reexportar o PDF para obter o QR Code novo.
    /// </summary>
    [HttpPost("{id:guid}/rotate-token")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateQrToken([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RotateTableQrTokenCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Reativa uma mesa desativada.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetTableActiveCommand(id, true), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Desativa uma mesa sem excluí-la — alternativa oferecida quando a exclusão é recusada por
    /// haver histórico (cenário Gherkin "Exclusão de mesa com histórico").
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetTableActiveCommand(id, false), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Exclui (soft delete) uma mesa sem sessões no histórico. Recusado (422) quando há histórico
    /// — o gestor deve desativar em vez de excluir.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteTableCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cenário Gherkin "Exportação para impressão": PDF com um QR Code por página, identificado
    /// pelo rótulo da mesa. <paramref name="areaId"/> nulo exporta todas as mesas ativas do
    /// tenant. Exige <c>TableManage</c> (não só leitura) porque o PDF embute o <c>qr_token</c> —
    /// o mesmo segredo de entrada que nunca é exposto em JSON puro pelos outros endpoints deste
    /// controller.
    /// </summary>
    [HttpGet("qr-codes.pdf")]
    [Authorize(Policy = "TableManage")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExportQrCodesPdf([FromQuery] Guid? areaId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ExportTablesQrCodesPdfQuery(areaId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult(HttpContext);
        }

        return File(result.Value!.Content, "application/pdf", result.Value.FileName);
    }
}
