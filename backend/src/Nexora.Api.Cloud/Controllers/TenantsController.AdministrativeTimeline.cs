using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Tenants.Queries.GetAdministrativeTimeline;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — linha do tempo administrativa de
/// UM estabelecimento. Partial file NOVO (ver docstring de <see cref="TenantsController"/>) para não
/// colidir com <c>TenantsController.Plan.cs</c>/<c>TenantsController.Ownership.cs</c>/
/// <c>TenantsController.Deployment.cs</c>, propriedade de outras histórias da E-15. Mesma policy
/// <c>PlatformAdmin</c> de <see cref="TenantsController.Overview"/> — sem equivalente self-service.
/// </summary>
public partial class TenantsController
{
    private const int DefaultTimelineLimit = 50;

    /// <summary>Gherkin "Linha do tempo administrativa" — fatos em ordem cronológica, com ator/origem/motivo/correlationId quando aplicável.</summary>
    [HttpGet("{id:guid}/administrative-timeline")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(AdministrativeTimelineListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdministrativeTimeline(
        [FromRoute] Guid id,
        [FromQuery(Name = "type")] AdministrativeTimelineEntryType[]? type,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? actorId,
        [FromQuery] string? correlationId,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = new GetAdministrativeTimelineQuery(
            id,
            type ?? Array.Empty<AdministrativeTimelineEntryType>(),
            from,
            to,
            limit ?? DefaultTimelineLimit,
            cursor,
            actorId,
            correlationId);

        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
