using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Alerts.Commands.UpdateAlertRouting;
using Nexora.Application.Alerts.Queries.GetAlertRouting;
using Nexora.Contracts.Alerts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>US-082 §7 <c>GET/PATCH /v1/tenant/alert-routing</c> — mesmo racional de <see cref="TenantThresholdsController"/>.</summary>
[ApiController]
[Authorize]
[Route("v1/tenant/alert-routing")]
public sealed class AlertRoutingController : ControllerBase
{
    private readonly ISender _sender;

    public AlertRoutingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, AlertRoutingRuleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAlertRoutingQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPatch]
    [Authorize(Policy = "ConfigWrite")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, AlertRoutingRuleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromBody] Dictionary<string, AlertRoutingRulePatch> patch, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateAlertRoutingCommand(patch), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
