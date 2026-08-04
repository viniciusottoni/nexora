using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Alerts.Commands.UpdateTenantThresholds;
using Nexora.Application.Alerts.Queries.GetTenantThresholds;
using Nexora.Contracts.Alerts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-080 §7 <c>GET/PATCH /v1/tenant/thresholds</c> — autoridade de escrita (US-080 cabeçalho
/// "Autoridade do dado: Nuvem"). PATCH exige "config:write" (mesma policy de <c>BrandingController</c>
/// — thresholds é mais uma seção de <c>tenant_config</c>); GET fica aberto a qualquer autenticado do
/// tenant, mesmo racional de <c>TenantThresholdsController</c> do edge.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/tenant/thresholds")]
public sealed class TenantThresholdsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantThresholdsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantThresholdsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantThresholdsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPatch]
    [Authorize(Policy = "ConfigWrite")]
    [ProducesResponseType(typeof(TenantThresholdsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdateTenantThresholdsRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateTenantThresholdsCommand(request), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
