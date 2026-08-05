using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.UpdateTenantPlan;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — única porta de escrita para agendar/aplicar
/// uma mudança de plano comercial. Grava <see cref="TenantPlanHistory"/> (sempre, mesmo para
/// vigência futura — "a mudança deve aparecer no histórico" independente de já ter sido efetivada),
/// <see cref="AuditLog"/> (RN-004) e, quando a vigência já chegou, emite <c>tenant.plan_changed</c>
/// (EVT-057) e reconcilia <c>tenant_config</c> com as capacidades do novo plano (EVT-054), tudo na
/// MESMA transação (<c>TransactionBehavior</c>, ADR-006) — mesmo padrão de
/// <c>TransitionTenantStatusCommandHandler</c>.
/// </summary>
internal sealed class UpdateTenantPlanCommandHandler
    : IRequestHandler<UpdateTenantPlanCommand, Result<TenantPlanUpdateResponse>>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantPlanCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantPlanUpdateResponse>> Handle(
        UpdateTenantPlanCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<TenantPlanUpdateResponse>.Failure(
                "Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // Concorrência (mesmo padrão de TransitionTenantStatusCommandHandler) — checada antes de
        // qualquer outra regra de negócio.
        if (tenant.PlanVersion != request.ExpectedVersion)
        {
            return Result<TenantPlanUpdateResponse>.Failure(
                "Este estabelecimento foi alterado por outra sessão. Recarregue e tente novamente.",
                ApiErrorCodes.ConcurrencyConflict);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<TenantPlanUpdateResponse>.Failure(
                "O motivo da mudança de plano é obrigatório.", ApiErrorCodes.ReasonRequired);
        }

        // US-154 §4 cenário "Plano desconhecido" — checado ANTES de qualquer escrita: "nenhuma
        // configuração parcial deve ser aplicada". Código desativado também é recusado (RN-016).
        var normalizedPlanCode = request.Plan.Trim().ToUpperInvariant();
        var platformPlan = await _db.PlatformPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == normalizedPlanCode && p.IsActive, cancellationToken);

        if (platformPlan is null)
        {
            return Result<TenantPlanUpdateResponse>.Failure(
                "Plano comercial não disponível.", ApiErrorCodes.PlanNotAvailable);
        }

        var now = DateTimeOffset.UtcNow;
        var effectiveAt = request.EffectiveAt ?? now;

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var (previousPlan, appliedImmediately) = tenant.SchedulePlanChange(platformPlan.Code, effectiveAt, now);

        var history = TenantPlanHistory.Create(
            tenant.Id,
            previousPlan,
            platformPlan.Code,
            request.Reason,
            requestedAt: now,
            effectiveAt: effectiveAt,
            actorId: request.ActorId);
        _db.TenantPlanHistories.Add(history);

        TenantPlanScheduledResponse? scheduled = null;

        if (appliedImmediately)
        {
            var planChangedEvent = DomainEvent.Create(
                tenant.Id,
                type: "tenant.plan_changed",
                aggregateType: "tenant",
                aggregateId: tenant.Id,
                payload: JsonSerializer.Serialize(new
                {
                    tenantId = tenant.Id,
                    previousPlan,
                    plan = platformPlan.Code,
                    effectiveAt,
                    actorId = request.ActorId,
                }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: request.ActorId);
            _db.DomainEvents.Add(planChangedEvent);

            history.MarkApplied(now, planChangedEvent.Id);

            _db.AuditLogs.Add(AuditLog.Create(
                tenant.Id,
                action: "TENANT_PLAN_CHANGED",
                entity: "tenant",
                occurredAt: now,
                actorId: request.ActorId,
                entityId: tenant.Id,
                before: JsonSerializer.Serialize(new { plan = previousPlan }),
                after: JsonSerializer.Serialize(new { plan = platformPlan.Code }),
                reason: request.Reason,
                domainEventId: planChangedEvent.Id));

            // EVT-054 — reconcilia as capacidades efetivas com o novo plano na MESMA transação.
            var tenantConfig = await _db.TenantConfigs
                .SingleOrDefaultAsync(c => c.TenantId == tenant.Id, cancellationToken);

            if (tenantConfig is not null)
            {
                tenantConfig.ApplyPlanCapabilities(platformPlan.CapabilitiesJson, platformPlan.Version);

                _db.DomainEvents.Add(DomainEvent.Create(
                    tenant.Id,
                    type: "tenant.config_updated",
                    aggregateType: "tenant",
                    aggregateId: tenant.Id,
                    payload: JsonSerializer.Serialize(new { configVersion = tenantConfig.ConfigVersion, source = "PLAN" }),
                    origin: "CLOUD",
                    occurredAt: now,
                    actorId: request.ActorId));
            }
        }
        else
        {
            // RN-004 — o agendamento em si já é uma ação sensível com autor e antes/depois, mesmo
            // antes de entrar em vigor; a efetivação em si gera seu PRÓPRIO audit_log mais tarde
            // (ver GetTenantPlanQueryHandler), correlacionado ao domain_event daquele momento.
            _db.AuditLogs.Add(AuditLog.Create(
                tenant.Id,
                action: "TENANT_PLAN_CHANGE_SCHEDULED",
                entity: "tenant",
                occurredAt: now,
                actorId: request.ActorId,
                entityId: tenant.Id,
                before: JsonSerializer.Serialize(new { plan = previousPlan }),
                after: JsonSerializer.Serialize(new { plan = platformPlan.Code, effectiveAt }),
                reason: request.Reason));

            scheduled = new TenantPlanScheduledResponse(platformPlan.Code, effectiveAt);
        }

        var response = new TenantPlanUpdateResponse(tenant.Plan, scheduled, tenant.PlanVersion);

        return Result<TenantPlanUpdateResponse>.Success(response);
    }
}
