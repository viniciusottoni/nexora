using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Installations.Queries.Platform.GetInstallationDiagnostics;
using Nexora.Application.Installations.Queries.Platform.ListInstallationIncidents;
using Nexora.Application.Installations.Queries.Platform.ListPlatformInstallations;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-140 "Painel de instalações com saúde" — módulo de plataforma, opera deliberadamente sobre o
/// parque inteiro (exceção legítima do ADR-013, mesmo raciocínio de <see cref="TenantsController"/>).
/// Distinto de <see cref="InstallationsController"/>: aquele é voltado ao PRÓPRIO dispositivo edge
/// (autenticação por assinatura Ed25519, rotas de bootstrap/sync/heartbeat); este é voltado ao
/// operador humano da plataforma (JWT + policy <c>PlatformAdmin</c>), só leitura, e nunca expõe
/// dado de negócio de tenant (RN-015).
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("v1/platform/installations")]
public partial class PlatformInstallationsController : ControllerBase
{
    private readonly ISender _sender;

    public PlatformInstallationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>US-140 §4 "Visão consolidada" — todas as instalações do parque com saúde, versão, atraso de sincronização.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PlatformInstallationListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListPlatformInstallationsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-140 §4 "Diagnóstico remoto" — health check + logs recentes (RN-015: nunca dado de negócio).</summary>
    [HttpGet("{id:guid}/diagnostics")]
    [ProducesResponseType(typeof(InstallationDiagnosticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Diagnostics([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetInstallationDiagnosticsQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-140 §4 "Histórico de incidentes" — duração e causa de cada incidente, mais recente primeiro.</summary>
    [HttpGet("{id:guid}/incidents")]
    [ProducesResponseType(typeof(InstallationIncidentListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Incidents([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListInstallationIncidentsQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
