using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Installation.Commands.ImportBootstrap;
using Nexora.Application.Installation.Queries.GetInstallationHealth;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Installation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Bootstrap de primeira subida e saúde do servidor edge — porta de
/// <c>import-bootstrap.ts</c> e <c>installation-health.controller.ts</c>. Ambas as rotas são
/// públicas: o bootstrap roda antes de qualquer credencial existir localmente (ADR-004, "uma
/// loja = um tenant" no edge — não há JWT possível ainda) e o health check é consultado por
/// ferramentas de operação/monitoramento sem autenticação, igual ao <c>@Public()</c> original.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class InstallationController : ControllerBase
{
    private readonly ISender _sender;

    public InstallationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Importa a carga de primeira subida (identidade do tenant/loja, configuração e, quando os
    /// módulos correspondentes existirem, catálogo/autorização — ver
    /// <c>IBootstrapCatalogImporter</c>/<c>IBootstrapAuthorizationImporter</c>). Chamado uma
    /// única vez pelo processo de instalação/compose, não pelo próprio app no startup (decisão
    /// de design: mantém o processo principal livre de I/O de arquivo arbitrário — ver
    /// <c>ImportBootstrapRequest</c>).
    /// </summary>
    [HttpPost("v1/installation/bootstrap/import")]
    [ProducesResponseType(typeof(ImportBootstrapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportBootstrap(
        [FromBody] ImportBootstrapRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new ImportBootstrapCommand(request.Tenant, request.Store, request.Installation, request.ConfigPages);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Saúde do servidor local — Postgres, Redis, última sincronização, versão.</summary>
    [HttpGet("v1/health")]
    [ProducesResponseType(typeof(InstallationHealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetInstallationHealthQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
