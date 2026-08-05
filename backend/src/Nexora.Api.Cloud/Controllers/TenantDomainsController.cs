using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.TenantDomains.Commands.RegisterTenantDomain;
using Nexora.Application.TenantDomains.Commands.VerifyTenantDomain;
using Nexora.Application.TenantDomains.Queries.ListTenantDomains;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Domínio próprio por cliente (US-143) — cadastro, verificação de posse por DNS e listagem.
/// Módulo de plataforma/infra (a US não lista self-service do cliente, ao contrário da US-141):
/// as três rotas exigem <c>PlatformAdmin</c>, mesma exceção legítima do ADR-013 usada por
/// <c>TenantsController</c>.
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("v1/platform")]
public sealed class TenantDomainsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantDomainsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Cadastra um domínio próprio para o tenant e devolve as instruções de verificação DNS (US-143 §7).</summary>
    [HttpPost("tenants/{id:guid}/domains")]
    [ProducesResponseType(typeof(RegisterTenantDomainResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromRoute(Name = "id")] Guid tenantId,
        [FromBody] RegisterTenantDomainRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterTenantDomainCommand(tenantId, request.Domain), cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Tenta verificar a posse do domínio pelo registro TXT (US-143 §7, cenários "Verificação de propriedade"/"Domínio não verificado").</summary>
    [HttpPost("domains/{id:guid}/verify")]
    [ProducesResponseType(typeof(VerifyTenantDomainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Verify(
        [FromRoute(Name = "id")] Guid domainId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyTenantDomainCommand(domainId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Lista domínios — de todos os tenants (visão de plataforma) ou de um único tenant, via <c>?tenantId=</c>.</summary>
    [HttpGet("domains")]
    [ProducesResponseType(typeof(TenantDomainListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListTenantDomainsQuery(tenantId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
