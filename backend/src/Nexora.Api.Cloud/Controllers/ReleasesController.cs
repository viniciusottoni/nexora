using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Releases.Commands.PublishRelease;
using Nexora.Application.Releases.Queries.GetReleaseRollout;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Publicação e acompanhamento de rollout de versão do software de edge (US-146 — "Atualização
/// controlada do parque"). Módulo de plataforma — opera deliberadamente sobre vários tenants ao
/// mesmo tempo (exceção legítima do ADR-013), mesma natureza de <see cref="TenantsController"/>.
/// ADR-019: a atualização é PUXADA pelo edge (ver <c>RunEdgeUpdateCycleCommand</c>/
/// <c>EdgeUpdateCycleWorker</c>, Api.Edge) — esta API só declara o que está disponível e para qual
/// fatia do parque, nunca empurra nada para uma instalação.
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("v1/platform/releases")]
public sealed class ReleasesController : ControllerBase
{
    private readonly ISender _sender;

    public ReleasesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Publica uma versão nova ou amplia a liberação gradual de uma já publicada (US-146 §3.1).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PublishReleaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publish(
        [FromBody] PublishReleaseRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new PublishReleaseCommand(request.Version, request.RolloutPercent, request.Notes);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Progresso da liberação de uma versão no parque (US-146 §7/§10).</summary>
    [HttpGet("{version}/rollout")]
    [ProducesResponseType(typeof(ReleaseRolloutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rollout([FromRoute] string version, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetReleaseRolloutQuery(version), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
