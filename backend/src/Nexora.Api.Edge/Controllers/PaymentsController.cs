using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Cashier.Commands.RegisterPayments;
using Nexora.Contracts.Cashier;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Recebimento de pagamento (US-052, Múltiplas formas de pagamento na mesma conta; US-058,
/// Pagamento de maquininha externa) — autoridade do dado é local (RN-005), controller próprio
/// (não em <see cref="TableSessionsController"/>) porque fecha a comanda por completo, diferente
/// do pagamento PARCIAL de US-027 (<c>POST /v1/sessions/{id}/bill/partial-payment</c>, que
/// permanece nesse outro controller).
/// </summary>
[ApiController]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// US-052 §4, cenário "Três formas na mesma conta" — recebe o conjunto INTEIRO de pagamentos
    /// numa única chamada (confirmação única, não uma por forma, US-052 §10). US-058: cada item do
    /// corpo pode carregar <c>provider</c>/<c>providerRef</c>/<c>brand</c>/<c>installments</c>.
    /// </summary>
    [HttpPost("v1/sessions/{id:guid}/payments")]
    [Authorize(Policy = "PaymentRegister")]
    [ProducesResponseType(typeof(RegisterPaymentsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromRoute] Guid id,
        [FromBody] RegisterPaymentsRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        var payments = request.Payments
            .Select(p => new PaymentInput(
                p.Method, p.Amount, p.ReceivedAmount, p.Provider, p.ProviderRef, p.Brand, p.Installments, p.ConfirmDuplicate))
            .ToList();

        var result = await _sender.Send(new RegisterPaymentsCommand(id, payments, occurredAt), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }
}
