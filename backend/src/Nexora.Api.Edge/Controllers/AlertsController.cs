using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Alerts.Commands.AcknowledgeAlert;
using Nexora.Application.Alerts.Commands.ResolveAlert;
using Nexora.Application.Alerts.Queries.GetAlerts;
using Nexora.Contracts.Alerts;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// E-08/US-080 §7, US-081 §7, US-083 §7 — consulta e reconhecimento de alertas operacionais.
/// Autoridade do dado é local (US-080 cabeçalho "Autoridade do dado: Local (avaliação)"): o motor
/// (<c>AlertEvaluationWorker</c>) roda no edge, então é aqui que os alertas operacionais existem
/// primeiro. Qualquer usuário autenticado do tenant pode consultar/reconhecer — o direcionamento
/// (US-082, <c>TargetRoles</c>/<c>TargetUserId</c>) já restringe QUEM recebe a notificação; a leitura
/// posterior não precisa de uma policy própria (mesmo racional de "listagem fora da policy" já usado
/// em TableManage/DeviceManage).
/// </summary>
[ApiController]
[Authorize]
[Route("v1")]
public sealed class AlertsController : ControllerBase
{
    private readonly ISender _sender;

    public AlertsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>US-080 §7 <c>GET /v1/alerts?status=open</c> e US-083 §7 <c>GET /v1/alerts?grouped=true</c>.</summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(AlertListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AlertGroupListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool grouped, CancellationToken cancellationToken)
    {
        if (grouped)
        {
            var groupedResult = await _sender.Send(new GetGroupedAlertsQuery(), cancellationToken);
            return groupedResult.ToActionResult(HttpContext);
        }

        var result = await _sender.Send(new GetOpenAlertsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-081 §7 <c>GET /v1/notifications?status=unread</c> — mesma consulta, restrita ao usuário autenticado (central de notificações, US-081 §3).</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(AlertListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNotifications(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOpenAlertsQuery(OnlyForCurrentUser: true), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-081 §7 <c>POST /v1/alerts/{id}/acknowledge</c>.</summary>
    [HttpPost("alerts/{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcknowledgeAlertCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-080 §7 <c>POST /v1/alerts/{id}/resolve</c>.</summary>
    [HttpPost("alerts/{id:guid}/resolve")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ResolveAlertCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
