using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Cashier.Commands.WaiveSessionServiceFee;
using Nexora.Contracts.Cashier;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// US-053 (Taxa de serviço com retirada registrada) — <c>POST /v1/sessions/{id}/service-fee/waive</c>.
/// Registro AUTORITATIVO da retirada (RN-010), diferente da retirada efêmera por pessoa de US-027
/// (<c>POST /v1/sessions/{id}/bill/waive-service-fee</c>, que continua em <see cref="TableSessionsController"/>).
/// </summary>
[ApiController]
[Authorize]
public sealed class ServiceFeeController : ControllerBase
{
    private readonly ISender _sender;

    public ServiceFeeController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>US-053 §4 — sem exigência de autorização de perfil superior (§10: "travar isso atrapalha o caixa em situação corriqueira").</summary>
    [HttpPost("v1/sessions/{id:guid}/service-fee/waive")]
    [Authorize(Policy = "BillManage")]
    [ProducesResponseType(typeof(WaiveSessionServiceFeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Waive(
        [FromRoute] Guid id,
        [FromBody] WaiveSessionServiceFeeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new WaiveSessionServiceFeeCommand(id, request.Reason, request.Scope, request.Person), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
