using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Cashier.Commands.PrintReceipt;
using Nexora.Application.Cashier.Commands.ReprintReceipt;
using Nexora.Application.Cashier.Queries.GetReceipt;
using Nexora.Contracts.Cashier;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// US-057 (Comprovante não fiscal de consumo) — geração, impressão térmica (ADR-026) e reimpressão
/// auditada. <see cref="ReceiptResponse.IsFiscal"/> é sempre <c>false</c>: RN-023 (emissão fiscal)
/// é pendência crítica fora de escopo desta wave.
/// </summary>
[ApiController]
[Authorize]
public sealed class ReceiptController : ControllerBase
{
    private readonly ISender _sender;

    public ReceiptController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("v1/sessions/{id:guid}/receipt")]
    [Authorize(Policy = "TableRead")]
    [ProducesResponseType(typeof(GetReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetReceiptQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-057 §4, cenário "Impressora indisponível": sempre 202, nunca bloqueia o fluxo do caixa.</summary>
    [HttpPost("v1/sessions/{id:guid}/receipt/print")]
    [Authorize(Policy = "BillManage")]
    [ProducesResponseType(typeof(PrintReceiptResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Print(
        [FromRoute] Guid id, [FromBody] PrintReceiptRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PrintReceiptCommand(id, request.PrinterId), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status202Accepted, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-057 §4, cenário "Reimpressão auditada" — registrada em <c>audit_log</c> com autor e horário.</summary>
    [HttpPost("v1/sessions/{id:guid}/receipt/reprint")]
    [Authorize(Policy = "BillManage")]
    [ProducesResponseType(typeof(PrintReceiptResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Reprint(
        [FromRoute] Guid id, [FromBody] PrintReceiptRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReprintReceiptCommand(id, request.PrinterId), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status202Accepted, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }
}
