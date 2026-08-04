using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Alerts.Commands.SubscribePush;
using Nexora.Application.Alerts.Queries.GetAlerts;
using Nexora.Contracts.Alerts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-081 §7 — sempre na nuvem (§2 "o push é enviado pela nuvem"). <c>POST /v1/notifications/subscribe</c>
/// registra a assinatura; <c>GET /v1/notifications?status=unread</c> é a central de notificações do
/// usuário autenticado.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(SubscribePushResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribePushRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubscribePushCommand(request.Endpoint, request.Keys.P256dh, request.Keys.Auth), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpGet]
    [ProducesResponseType(typeof(AlertListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOpenAlertsQuery(OnlyForCurrentUser: true), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
