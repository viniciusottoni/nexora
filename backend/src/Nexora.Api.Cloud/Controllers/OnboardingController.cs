using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Onboarding.Commands.ActivateTenant;
using Nexora.Application.Onboarding.Commands.CompleteOnboardingStep;
using Nexora.Application.Onboarding.Queries.GetOnboardingStatus;
using Nexora.Application.Tenants.Commands.RecordCrossTenantAccessAttempt;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Roteiro de implantação autoatendido (US-141) — checklist rastreável dos nove passos e ativação
/// do tenant. Controller NOVO, deliberadamente separado de <c>TenantsController</c> (instrução da
/// tarefa: não editar aquele arquivo em paralelo).
/// </summary>
/// <remarks>
/// Acesso duplo em <see cref="Get"/> e <see cref="CompleteStep"/> espelha exatamente
/// <c>TenantsController.Get</c> (US-001, cenário "Tentativa de acesso cruzado por ID"): a policy
/// <c>PlatformAdmin</c> (Replay) pode consultar/agir sobre qualquer tenant; o usuário comum só sobre
/// o PRÓPRIO tenant (<c>ICurrentTenantContext.TenantId == id</c>) — essencial aqui porque US-141 §3.1
/// exige autoatendimento pelo gestor do cliente (P8), não só visão da Replay (P9). ID de outro tenant
/// nunca vira 403 (ADR-021): vira 404 e a tentativa é auditada. <see cref="Activate"/> é
/// deliberadamente só <c>PlatformAdmin</c> — ativar o estabelecimento é uma decisão da Replay, não
/// self-service do cliente (US-141 §1: "para que a Replay implante em escala").
/// </remarks>
[ApiController]
[Authorize]
[Route("v1/platform/tenants")]
public sealed class OnboardingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentTenantContext _tenantContext;

    public OnboardingController(ISender sender, IAuthorizationService authorizationService, ICurrentTenantContext tenantContext)
    {
        _sender = sender;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
    }

    /// <summary>Checklist de implantação com os nove passos e o estado de cada um (US-141 §4, cenário "Checklist de implantação").</summary>
    [HttpGet("{id:guid}/onboarding")]
    [ProducesResponseType(typeof(OnboardingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var denied = await AuthorizeTenantAccessAsync(id, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        var result = await _sender.Send(new GetOnboardingStatusQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Conclusão manual de um passo (US-141 §3.1 "assistente de configuração inicial no painel do
    /// cliente") — usado ao menos por <c>TRAINING</c>/<c>PILOT</c> (sem sinal automático) e como
    /// fallback para os demais passos derivados.
    /// </summary>
    [HttpPatch("{id:guid}/onboarding/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompleteStep(
        [FromRoute] Guid id,
        [FromRoute] string key,
        CancellationToken cancellationToken)
    {
        var denied = await AuthorizeTenantAccessAsync(id, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        if (!OnboardingStepKeyWireFormat.TryParseWireKey(key, out var stepKey))
        {
            return Result.Failure("Passo do roteiro de implantação não encontrado.", ApiErrorCodes.OnboardingStepNotFound)
                .ToActionResult(HttpContext);
        }

        var command = new CompleteOnboardingStepCommand(id, stepKey, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Ativação ao final do roteiro (US-141 §4, cenário "Validação antes da ativação") — só a Replay decide ativar.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateTenantCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Mesma lógica de <c>TenantsController.Get</c> — devolve um <see cref="IActionResult"/> de 404 quando o acesso deve ser negado, ou <c>null</c> quando pode prosseguir.</summary>
    private async Task<IActionResult?> AuthorizeTenantAccessAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var isPlatformAdmin = (await _authorizationService.AuthorizeAsync(User, "PlatformAdmin")).Succeeded;
        var isOwnTenant = _tenantContext.TenantId == tenantId;

        if (isPlatformAdmin || isOwnTenant)
        {
            return null;
        }

        if (_tenantContext.TenantId is { } actorTenantId)
        {
            await _sender.Send(
                new RecordCrossTenantAccessAttemptCommand(
                    actorTenantId,
                    _tenantContext.UserId,
                    tenantId,
                    HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);
        }

        return Result.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound).ToActionResult(HttpContext);
    }
}
