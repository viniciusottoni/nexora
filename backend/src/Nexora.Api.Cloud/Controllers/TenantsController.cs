using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.Application.Tenants.Commands.RecordCrossTenantAccessAttempt;
using Nexora.Application.Tenants.Commands.TransitionTenantStatus;
using Nexora.Application.Tenants.Queries.CheckTenantSlugAvailability;
using Nexora.Application.Tenants.Queries.GetTenantById;
using Nexora.Application.Tenants.Queries.GetTenantOverview;
using Nexora.Application.Tenants.Queries.ListTenants;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
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
public partial class TenantsController : ControllerBase
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

    private const int DefaultDirectoryLimit = 25;

    /// <summary>
    /// Diretório de estabelecimentos com busca e filtros (US-151). Filtros repetíveis chegam como
    /// array vinculado pelo model binder padrão do ASP.NET Core (<c>?status=ACTIVE&amp;status=TRIAL</c>);
    /// <see cref="TenantStatus"/>/<see cref="TenantHealthStatus"/>/<see cref="TenantDirectorySort"/>
    /// já são vinculados como enum (<c>Enum.TryParse(ignoreCase: true)</c> do binder padrão aceita
    /// os rótulos em caixa alta/camelCase do contrato sem tradução manual aqui) — um valor inválido
    /// vira 400 automático (<c>[ApiController]</c>, model state inválido), nunca alcança o handler.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantDirectoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? query,
        [FromQuery(Name = "status")] TenantStatus[]? status,
        [FromQuery(Name = "plan")] string[]? plan,
        [FromQuery(Name = "template")] string[]? template,
        [FromQuery(Name = "health")] TenantHealthStatus[]? health,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] TenantDirectorySort? sort,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var listQuery = new ListTenantsQuery(
            query,
            status ?? Array.Empty<TenantStatus>(),
            plan ?? Array.Empty<string>(),
            template ?? Array.Empty<string>(),
            health ?? Array.Empty<TenantHealthStatus>(),
            createdFrom,
            createdTo,
            sort ?? TenantDirectorySort.Attention,
            limit ?? DefaultDirectoryLimit,
            cursor);

        var result = await _sender.Send(listQuery, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Visão 360 administrativa do estabelecimento (US-152) — metadados de cadastro, dono, lojas,
    /// instalações, checklist de implantação e links, sem nenhum dado operacional (RN-015).
    /// Exclusivo do administrador de plataforma (P9): diferente de <see cref="Get"/>, não existe
    /// self-service equivalente aqui (US-152 §1 "como administrador da plataforma"), mesma policy de
    /// <see cref="List"/>/<see cref="Create"/>.
    /// </summary>
    [HttpGet("{id:guid}/overview")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantOverviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Overview([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantOverviewQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-153 · Ciclo de vida do estabelecimento — suspende, reativa ou cancela um estabelecimento.
    /// Concorrência otimista via <c>If-Match</c> (doc §7) — obrigatório; ausente ou não numérico é
    /// tratado como <c>0</c>, que nunca combina com <see cref="Tenant.StatusVersion"/> real (começa
    /// em 1), gerando 409 CONCURRENCY_CONFLICT em vez de aplicar a transição às cegas. Mesma policy
    /// de <see cref="List"/>/<see cref="Create"/>/<see cref="Overview"/> — sem equivalente
    /// self-service (só a plataforma decide o ciclo de vida comercial).
    /// </summary>
    [HttpPost("{id:guid}/status-transitions")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantStatusTransitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TransitionStatus(
        [FromRoute] Guid id,
        [FromBody] TenantStatusTransitionRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var command = new TransitionTenantStatusCommand(
            id,
            request.TargetStatus,
            request.Reason,
            request.EffectiveAt,
            ParseExpectedVersion(ifMatch),
            _tenantContext.UserId);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Extrai a versão esperada do <c>If-Match</c> (aspas de ETag removidas) — ausente/malformado vira o sentinela 0 (ver docstring de <see cref="TransitionStatus"/>).</summary>
    private static int ParseExpectedVersion(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return 0;
        }

        var trimmed = ifMatch.Trim().Trim('"');
        return int.TryParse(trimmed, out var version) ? version : 0;
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
