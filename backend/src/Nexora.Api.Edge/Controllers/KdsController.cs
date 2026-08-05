using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Orders.Commands.AdvanceKdsItem;
using Nexora.Application.Orders.Commands.AdvanceKdsOrder;
using Nexora.Application.Orders.Commands.UndoKdsItemAdvance;
using Nexora.Application.Orders.Queries.GetKdsHistory;
using Nexora.Application.Orders.Queries.GetKdsQueue;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Fila do KDS por praça e avanço/desfazer por teclado numérico (US-040/US-041, ADR-011/ADR-020).
/// <c>GET /v1/kds/queue</c> também é o fallback de polling do ADR-011, reaproveitado por
/// <c>KdsHub.Resume</c> na reconexão. O avanço "clássico" por <c>orderId+itemId</c>
/// (<c>AdvanceOrderItemStatusCommand</c>, US-024) continua exposto em
/// <c>OrderItemsController</c> — as rotas daqui são as do CONTRATO do KDS (US-041 §7:
/// <c>/v1/kds/items/{id}/advance</c>, <c>/v1/kds/orders/{code}/advance</c>,
/// <c>/v1/kds/items/{id}/undo</c>), que não exigem o operador (ou o teclado) conhecer o
/// <c>orderId</c>.
/// </summary>
[ApiController]
[Route("v1/kds")]
[Authorize]
public sealed class KdsController : ControllerBase
{
    private readonly ISender _sender;

    public KdsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Policy própria (<c>KdsQueueRead</c>, registrada em <c>Program.cs</c>) em vez de reaproveitar
    /// <c>KdsAdvance</c>/<c>OrderRead</c> diretamente: quem só lê a fila (ex.: painel de
    /// acompanhamento) não precisa da permissão de AVANÇAR item.
    /// </summary>
    [HttpGet("queue")]
    [Authorize(Policy = "KdsQueueRead")]
    [ProducesResponseType(typeof(GetKdsQueueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Queue([FromQuery] Guid stationId, [FromQuery] string? since, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetKdsQueueQuery(stationId, since), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-041 §7 — avanço direto por toque no cartão (não pelo teclado numérico, que usa
    /// <see cref="AdvanceOrder"/>). <c>X-Occurred-At</c>: ver docstring de
    /// <c>AdvanceOrderItemStatusCommandHandler</c> (ADR-034).
    /// </summary>
    [HttpPost("items/{itemId:guid}/advance")]
    [Authorize(Policy = "KdsAdvance")]
    [ProducesResponseType(typeof(OrderItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdvanceItem(
        [FromRoute] Guid itemId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AdvanceKdsItemCommand(itemId, occurredAt), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-041 §1/§3/§7 — caminho principal do teclado numérico: código curto do pedido + Enter.
    /// Ver docstring completa de <see cref="AdvanceKdsOrderCommand"/> para a semântica de
    /// <see cref="AdvanceKdsOrderRequest.Batch"/>.
    /// </summary>
    [HttpPost("orders/{shortCode}/advance")]
    [Authorize(Policy = "KdsAdvance")]
    [ProducesResponseType(typeof(AdvanceKdsOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdvanceOrder(
        [FromRoute] string shortCode,
        [FromBody] AdvanceKdsOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AdvanceKdsOrderCommand(shortCode, request.StationId, request.Batch, occurredAt),
            cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-041 §3/§4 — desfazer o último avanço, janela de 10 s (<c>UndoKdsItemAdvanceCommandHandler.UndoWindow</c>).</summary>
    [HttpPost("items/{itemId:guid}/undo")]
    [Authorize(Policy = "KdsAdvance")]
    [ProducesResponseType(typeof(OrderItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Undo(
        [FromRoute] Guid itemId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UndoKdsItemAdvanceCommand(itemId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-046 (Histórico do turno no KDS) — itens já SERVIDOS da praça dentro do dia operacional
    /// corrente (ADR-018), do mais recente para o mais antigo, com busca opcional por código curto
    /// do pedido ou mesa. Mesma policy de leitura de <see cref="Queue"/> (<c>KdsQueueRead</c>): quem
    /// só consulta o histórico não precisa da permissão de AVANÇAR item. <c>shift</c> é aceito só
    /// por simetria com o contrato §7 da história — hoje o único turno suportado é o corrente,
    /// sempre calculado no handler a partir do relógio do servidor; o parâmetro não chega a
    /// <see cref="GetKdsHistoryQuery"/>.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = "KdsQueueRead")]
    [ProducesResponseType(typeof(GetKdsHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History(
        [FromQuery] Guid stationId,
        [FromQuery] string? shift,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetKdsHistoryQuery(stationId, search), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
