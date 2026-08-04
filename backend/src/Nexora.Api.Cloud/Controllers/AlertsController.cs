using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Alerts.Commands.AcknowledgeAlert;
using Nexora.Application.Alerts.Commands.ResolveAlert;
using Nexora.Application.Alerts.Queries.GetAlerts;
using Nexora.Contracts.Alerts;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// E-08/US-080 §7, US-083 §7 — mesmo contrato do gêmeo em <c>Nexora.Api.Edge</c>, aqui para os
/// alertas de gestão que nascem na nuvem (CASH_DIVERGENCE, SYNC_DELAY) e para o gestor consultar de
/// fora da loja (US-081 §2, "tipicamente o gestor fora da loja").
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

    [HttpPost("alerts/{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcknowledgeAlertCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

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
