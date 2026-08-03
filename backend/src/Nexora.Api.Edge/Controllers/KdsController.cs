using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Orders.Queries.GetKdsQueue;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Fila do KDS por praça (US-031, ADR-011) — hoje só o fallback de polling
/// (<c>GET /v1/kds/queue</c>, cenário Gherkin "Queda do WebSocket no KDS": "deve exibi-lo em no
/// máximo 5 segundos, via polling"), reaproveitado por <c>KdsHub.Resume</c> na reconexão. A
/// renderização completa da fila (US-040) e o avanço de item (<c>AdvanceOrderItemStatusCommand</c>,
/// já exposto por <c>OrderItemsController</c> — fora do escopo de arquivos desta história) continuam
/// noutro lugar.
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
}
