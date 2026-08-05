using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.Application.Tenants.Commands.RecordCrossTenantAccessAttempt;
using Nexora.Application.Tenants.Queries.CheckTenantSlugAvailability;
using Nexora.Application.Tenants.Queries.GetTenantById;
using Nexora.Application.Tenants.Queries.ListTenants;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Provisionamento e gestão de estabelecimentos (tenants) — módulo de plataforma, opera
/// deliberadamente sobre vários tenants ao mesmo tempo (exceção legítima do ADR-013).
/// Porta de <c>tenants.controller.ts</c>.
/// </summary>
/// <remarks>
/// <see cref="Create"/>, <see cref="SlugAvailability"/> e <see cref="List"/> exigem a policy
/// <c>PlatformAdmin</c> (definida em <c>Program.cs</c>) — 403 para quem não tem, o que é correto
/// aqui: não há "recurso" cuja existência se esconda, é uma ação de plataforma sem equivalente de
/// self-service. <see cref="Get"/> é diferente: um usuário comum pode legitimamente consultar o
/// PRÓPRIO estabelecimento (<c>ICurrentTenantContext.TenantId == id</c>); só quando o ID pedido é
/// de outro tenant é que a tentativa é auditada e a resposta vira 404 (nunca 403 — ADR-021, "não
/// revelar que o recurso existe em outro tenant").
/// </remarks>
[ApiController]
[Authorize]
[Route("v1/platform/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentTenantContext _tenantContext;

    public TenantsController(ISender sender, IAuthorizationService authorizationService, ICurrentTenantContext tenantContext)
    {
        _sender = sender;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
    }

    /// <summary>Provisiona um novo estabelecimento (RF-PLT).</summary>
    [HttpPost]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(ProvisionTenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] ProvisionTenantRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new ProvisionTenantCommand(
            request.Name,
            request.Slug,
            request.Plan,
            request.Template,
            request.Owner.Name,
            request.Owner.Email,
            request.Store.Name,
            request.Store.Timezone);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Get), new { id = result.Value!.Tenant.Id }, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Verifica se um endereço (slug) está livre antes de provisionar.</summary>
    [HttpGet("slug-availability")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(SlugAvailabilityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SlugAvailability([FromQuery] string slug, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckTenantSlugAvailabilityQuery(slug), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Lista todos os estabelecimentos provisionados.</summary>
    [HttpGet]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListTenantsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Retorna os dados de um estabelecimento específico. Qualquer usuário autenticado pode
    /// consultar o PRÓPRIO tenant; ID de outro tenant é tratado como recurso inexistente (404) e
    /// a tentativa é registrada em <c>audit_log</c> — só quem tem a policy <c>PlatformAdmin</c>
    /// pode consultar qualquer ID (US-001, cenário "Tentativa de acesso cruzado por ID").
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var isPlatformAdmin = (await _authorizationService.AuthorizeAsync(User, "PlatformAdmin")).Succeeded;
        var isOwnTenant = _tenantContext.TenantId == id;

        if (!isPlatformAdmin && !isOwnTenant)
        {
            if (_tenantContext.TenantId is { } actorTenantId)
            {
                await _sender.Send(
                    new RecordCrossTenantAccessAttemptCommand(
                        actorTenantId,
                        _tenantContext.UserId,
                        id,
                        HttpContext.Connection.RemoteIpAddress?.ToString()),
                    cancellationToken);
            }

            return Result<TenantSummaryResponse>
                .Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound)
                .ToActionResult(HttpContext);
        }

        var result = await _sender.Send(new GetTenantByIdQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
