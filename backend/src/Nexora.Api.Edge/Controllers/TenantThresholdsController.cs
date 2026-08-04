using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Alerts.Queries.GetTenantThresholds;
using Nexora.Contracts.Alerts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// US-080 §7 <c>GET /v1/tenant/thresholds</c> — só leitura no edge: a autoridade de escrita é a
/// nuvem (US-080 cabeçalho "Autoridade do dado: Nuvem (configuração dos limiares)"), o edge recebe
/// a atualização pelo pull de configuração (US-063) e só consome (<c>AlertEvaluationWorker</c>).
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
}
