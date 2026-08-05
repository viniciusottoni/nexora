using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Tenants.Commands.ReconcileTenantPlanConfig;
using Nexora.Application.Tenants.Commands.UpdateTenantPlan;
using Nexora.Application.Tenants.Queries.GetTenantPlan;
using Nexora.Application.Tenants.Queries.ListPlatformPlans;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — catálogo de planos comerciais e mudança de
/// plano de um estabelecimento com vigência e histórico. Partial file NOVO (ver docstring de
/// <see cref="TenantsController"/>) para não colidir com outras histórias da E-15 editando o
/// arquivo principal em paralelo. Todas as rotas exigem <c>PlatformAdmin</c>, mesma policy de
/// <see cref="TenantsController.List"/>/<see cref="TenantsController.Create"/>/<see cref="TenantsController.TransitionStatus"/>
/// — não há equivalente self-service (só a plataforma decide o plano comercial).
/// </summary>
public partial class TenantsController
{
    /// <summary>
    /// US-154 §7 — catálogo de planos comerciais. Rota ABSOLUTA (<c>/v1/platform/plans</c>, fora do
    /// prefixo <c>v1/platform/tenants</c> deste controller) — o "/" inicial faz o roteamento do
    /// ASP.NET Core ignorar o <see cref="RouteAttribute"/> de classe, mesmo mecanismo usado quando
    /// um recurso irmão precisa viver num controller já existente em vez de um novo arquivo de
    /// controller dedicado (decisão registrada no relatório final desta tarefa).
    /// </summary>
    [HttpGet("/v1/platform/plans")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(PlatformPlanListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPlans(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListPlatformPlansQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-154 §7 — plano atual, capacidades efetivas, agendamento pendente e consistência com o catálogo.</summary>
    [HttpGet("{id:guid}/plan")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantPlanQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-154 §7 — agenda/aplica upgrade ou downgrade de plano. <c>Idempotency-Key</c> (ADR-020,
    /// obrigatório em toda escrita) e <c>If-Match</c> (concorrência otimista sobre
    /// <see cref="Nexora.Domain.Platform.Tenant.PlanVersion"/>) seguem o mesmo padrão de
    /// <see cref="TenantsController.TransitionStatus"/>.
    /// </summary>
    [HttpPut("{id:guid}/plan")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantPlanUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdatePlan(
        [FromRoute] Guid id,
        [FromBody] TenantPlanUpdateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTenantPlanCommand(
            id,
            request.Plan,
            request.EffectiveAt,
            request.Reason,
            ParseExpectedVersion(ifMatch),
            _tenantContext.UserId);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Endpoint ADICIONAL ao contrato abreviado do §7 da US (ver docstring de
    /// <see cref="ReconcileTenantPlanConfigCommand"/>) — corrige explicitamente a divergência entre
    /// capacidades efetivas e o plano corrente, ação que a tela de detalhe oferece a partir do
    /// alerta de divergência (US-154 §4, cenário "Divergência detectada"). <c>Idempotency-Key</c>
    /// obrigatório (ADR-020, é uma escrita) — o comando em si já é idempotente por conteúdo (ver
    /// docstring do handler), a chave protege contra reenvio de rede duplicado.
    /// </summary>
    [HttpPost("{id:guid}/plan/reconciliations")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantPlanReconcileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReconcilePlanConfig(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ReconcileTenantPlanConfigCommand(id, _tenantContext.UserId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
