using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Orders.Queries.GetCurrentSessionConsumption;
using Nexora.Application.Tables.Queries.GetCurrentSessionBill;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Consumo da mesa em tempo real (US-024) — <c>GET /v1/public/sessions/current</c> SEM parâmetro
/// de sessão na rota (ver docstring de <see cref="GetCurrentSessionConsumptionQuery"/>): a única
/// sessão possível é a do token <c>TableSession</c> apresentado, o que por construção impede o
/// cenário Gherkin "Privacidade entre mesas" (token da mesa 12 nunca consegue nem tentar consultar
/// a mesa 13, porque não há id nenhum para trocar).
/// </summary>
[ApiController]
[Route("v1/public/sessions")]
[Authorize(Policy = "SessionScope")]
public sealed class PublicSessionConsumptionController : ControllerBase
{
    private readonly ISender _sender;

    public PublicSessionConsumptionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(SessionConsumptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentSessionConsumptionQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-027 §10 — pré-visualização da divisão pelo cliente ("Cliente pode pré-visualizar a
    /// divisão no celular antes de o caixa começar"). Só leitura: nenhuma escrita (atribuição por
    /// item, retirada de taxa, pagamento parcial) é exposta ao cliente final — essas continuam
    /// exclusivas do caixa, via <c>TableSessionsController</c>.
    /// </summary>
    [HttpGet("current/bill")]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CurrentBill(
        [FromQuery] string? split,
        [FromQuery] short? people,
        [FromQuery] decimal? amount,
        [FromQuery] string? waived,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentSessionBillQuery(split, people, amount, waived), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
