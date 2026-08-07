using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Tenants.Queries.GetTenantDeploymentStatus;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — checklist de implantação
/// reconstruído a partir de fatos persistidos, enriquecido com o estado de instalação/reemissão de
/// token que <see cref="TenantsController.Overview"/> (US-152) não expõe. Partial file NOVO (ver
/// docstring de <see cref="TenantsController"/>) para não colidir com outras histórias da E-15
/// editando o arquivo principal em paralelo. Mesma policy <c>PlatformAdmin</c> de
/// <see cref="TenantsController.Overview"/> — sem equivalente self-service.
/// </summary>
public partial class TenantsController
{
    [HttpGet("{id:guid}/deployment")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantDeploymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deployment([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantDeploymentStatusQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
