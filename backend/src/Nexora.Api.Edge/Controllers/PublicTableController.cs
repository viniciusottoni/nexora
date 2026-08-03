using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Tables.Commands.AccessTableByQrToken;
using Nexora.Application.Tables.Commands.CallWaiter;
using Nexora.Application.Tables.Commands.RequestBillByQr;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Ponto de entrada público do cliente do salão (US-021) — resolve a mesa pelo <c>qr_token</c>,
/// abre a sessão automaticamente quando ainda não há uma (US-022) e devolve o <c>sessionToken</c>
/// anônimo de escopo mínimo. Sem parâmetro de host: igual a <see cref="BrandingController"/>, o
/// edge é a autoridade operacional de um único tenant (ADR-004), então não precisa (nem deve)
/// resolver tenant a partir do domínio — o RLS já filtra pelo tenant fixo da instalação.
/// </summary>
[ApiController]
[Route("v1/public/table")]
public sealed class PublicTableController : ControllerBase
{
    private readonly ISender _sender;

    public PublicTableController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Cenários Gherkin da US-021 §4: "Acesso sem instalação", "Sessão de mesa já aberta",
    /// "Primeira leitura da mesa", "Token inválido ou rotacionado" e "Retorno após fechar o
    /// navegador". Sem <c>Idempotency-Key</c> (GET, não é uma escrita no sentido do ADR-020) — a
    /// idempotência aqui vem da própria regra de negócio (nunca duas sessões ativas na mesma
    /// mesa), não de uma chave gerada pelo cliente.
    /// </summary>
    [HttpGet("{qrToken}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicTableAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Access([FromRoute] string qrToken, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AccessTableByQrTokenCommand(qrToken), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-025 §7 — o cliente do salão chama o garçom. Autenticado pelo <c>sessionToken</c> anônimo
    /// (policy <c>SessionScope</c>, esquema <c>TableSession</c>): <c>sessionId</c>/<c>tableId</c>
    /// vêm das claims <c>ses</c>/<c>tbl</c> do PRÓPRIO token, nunca do corpo (RN-015) —
    /// <paramref name="qrToken"/> é conferido contra a mesa do token no handler; qualquer
    /// divergência devolve 404 genérico, nunca 403 (mesmo padrão de <c>RepeatOrderItem</c>).
    /// </summary>
    [HttpPost("{qrToken}/call-waiter")]
    [Authorize(Policy = "SessionScope")]
    [ProducesResponseType(typeof(CallWaiterResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CallWaiter(
        [FromRoute] string qrToken,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetSessionClaims(out var sessionId, out var tableId))
        {
            return NotFound();
        }

        var result = await _sender.Send(new CallWaiterCommand(qrToken, sessionId, tableId), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status202Accepted, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-026 §7 — o cliente do salão pede a conta, já escolhendo o modo de divisão. Mesmo esquema
    /// de autenticação/resolução de <see cref="CallWaiter"/> acima.
    /// </summary>
    [HttpPost("{qrToken}/request-bill")]
    [Authorize(Policy = "SessionScope")]
    [ProducesResponseType(typeof(RequestBillResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestBill(
        [FromRoute] string qrToken,
        [FromBody] RequestBillRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetSessionClaims(out var sessionId, out var tableId))
        {
            return NotFound();
        }

        var result = await _sender.Send(
            new RequestBillByQrCommand(qrToken, sessionId, tableId, request.SplitMode, request.People), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status202Accepted, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Lê as claims <c>ses</c>/<c>tbl</c> do token de sessão anônimo corrente (RN-015: nunca um
    /// parâmetro que o cliente informa) — <c>false</c> quando alguma claim falta/é inválida, o que
    /// não deveria acontecer para um token emitido por <c>JwtTokenIssuer.IssueTableSessionTokenAsync</c>,
    /// mas o chamador ainda assim devolve 404 genérico em vez de deixar propagar um erro de tipo.
    /// </summary>
    private bool TryGetSessionClaims(out Guid sessionId, out Guid tableId)
    {
        sessionId = Guid.Empty;
        tableId = Guid.Empty;

        var sessionClaim = User.FindFirst("ses")?.Value;
        var tableClaim = User.FindFirst("tbl")?.Value;

        return Guid.TryParse(sessionClaim, out sessionId) && Guid.TryParse(tableClaim, out tableId);
    }
}
