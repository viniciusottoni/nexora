using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Cashier.Commands.CloseCashSession;
using Nexora.Application.Cashier.Commands.OpenCashSession;
using Nexora.Application.Cashier.Commands.RegisterCashMovement;
using Nexora.Application.Cashier.Queries.GetCurrentCashSession;
using Nexora.Application.Cashier.Queries.ListCashMovements;
using Nexora.Contracts.Cashier;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Abertura/fechamento de caixa (US-055) e sangria/suprimento (US-056) — autoridade do dado é o edge
/// (cabeçalho "Autoridade do dado: Local" das duas USes), por isso este controller vive só em
/// <c>Nexora.Api.Edge</c>. Não recebe <c>cashSessionId</c> na maioria das rotas: a sessão corrente é
/// sempre resolvida por (loja, operador autenticado) — RN "um caixa por operador e turno".
/// </summary>
[ApiController]
[Authorize]
public sealed class CashSessionsController : ControllerBase
{
    private readonly ISender _sender;

    public CashSessionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Cenários Gherkin "Abertura com fundo" e "Um caixa por operador e turno" (US-055 §4).</summary>
    [HttpPost("v1/cash-sessions/open")]
    [Authorize(Policy = "CashOpen")]
    [ProducesResponseType(typeof(OpenCashSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Open(
        [FromBody] OpenCashSessionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new OpenCashSessionCommand(request.OpeningAmount), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, new OpenCashSessionResponse(result.Value!));
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-055 §7 — sessão aberta/em conferência do operador corrente, com a composição do valor esperado detalhada (US-055 §10).</summary>
    [HttpGet("v1/cash-sessions/current")]
    [Authorize(Policy = "CashRead")]
    [ProducesResponseType(typeof(GetCurrentCashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentCashSessionQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cenários Gherkin "Divergência no fechamento", "Fechamento sem divergência" e "Mesa aberta no fechamento" (US-055 §4).</summary>
    [HttpPost("v1/cash-sessions/{id:guid}/close")]
    [Authorize(Policy = "CashClose")]
    [ProducesResponseType(typeof(CloseCashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Close(
        [FromRoute] Guid id,
        [FromBody] CloseCashSessionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Authorization-Token")] string? authorizationToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CloseCashSessionCommand(id, request.CountedAmount, request.Justification, authorizationToken), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cenários Gherkin "Sangria registrada", "Suprimento de troco", "Sangria acima do limite" e "Movimento sem caixa aberto" (US-056 §4).</summary>
    [HttpPost("v1/cash-sessions/movements")]
    [Authorize(Policy = "CashMovement")]
    [ProducesResponseType(typeof(RegisterCashMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterMovement(
        [FromBody] RegisterCashMovementRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Authorization-Token")] string? authorizationToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCashMovementCommand(request.Type, request.Amount, request.Reason, authorizationToken), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-056 §7/§10 — histórico do turno acessível na mesma tela.</summary>
    [HttpGet("v1/cash-sessions/current/movements")]
    [Authorize(Policy = "CashRead")]
    [ProducesResponseType(typeof(ListCashMovementsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ListMovements(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListCashMovementsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
