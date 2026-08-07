using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Platform.Queries.GetPlatformSummary;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-150 "Estrutura e navegação do painel de plataforma" — resumo consumido pela raiz do
/// <c>web-platform</c> (visão geral). Só leitura, só contadores (RN-015); mesma policy exclusiva de
/// plataforma dos demais controllers globais (<see cref="TenantsController"/>,
/// <see cref="PlatformInstallationsController"/>).
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("v1/platform/summary")]
public sealed class PlatformSummaryController : ControllerBase
{
    private readonly ISender _sender;

    public PlatformSummaryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PlatformSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPlatformSummaryQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
