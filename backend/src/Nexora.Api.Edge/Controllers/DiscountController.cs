using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Cashier.Commands.ApplyDiscount;
using Nexora.Contracts.Cashier;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>US-054 (Desconto com autorização) — <c>POST /v1/sessions/{id}/discount</c>, RN-011.</summary>
[ApiController]
[Authorize]
public sealed class DiscountController : ControllerBase
{
    private readonly ISender _sender;

    public DiscountController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// US-054 §4 — desconto acima do limite configurado exige <c>X-Authorization-Token</c> válido
    /// para a ação <c>DISCOUNT_ABOVE_LIMIT</c> (ADR-023); abaixo do limite, aplica direto (mesmo
    /// espírito de "travar isso atrapalha o caixa em situação corriqueira", US-053 §10, reaplicado aqui).
    /// </summary>
    [HttpPost("v1/sessions/{id:guid}/discount")]
    [Authorize(Policy = "BillManage")]
    [ProducesResponseType(typeof(ApplyDiscountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Apply(
        [FromRoute] Guid id,
        [FromBody] ApplyDiscountRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Authorization-Token")] string? authorizationToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ApplyDiscountCommand(id, request.Percent, request.Amount, request.Reason, request.Scope, request.OrderItemId, authorizationToken),
            cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
