using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetTenantPlan;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial. Duas responsabilidades:
///
/// 1. Agregar a visão atual do plano (<c>current</c>/<c>effectiveCapabilities</c>/<c>scheduled</c>/
///    <c>consistent</c>/<c>version</c>).
/// 2. EFETIVAR de forma preguiçosa e idempotente uma mudança agendada (<see cref="TenantPlanHistory"/>
///    pendente) cuja <c>EffectiveAt</c> já passou — reconciliando <c>tenant_config</c> com as
///    capacidades do novo plano na MESMA operação (EVT-054 <c>tenant.config_updated</c>, <c>source:
///    "PLAN"</c>), exatamente como uma mudança de plano IMEDIATA já faz em
///    <c>UpdateTenantPlanCommandHandler</c>.
///
/// Decisão de design (documentada aqui e no relatório final da tarefa): a US pede vigência
/// (agendamento) mas não especifica QUEM aplica a mudança quando a data chega. Como não existe
/// hoje um <c>BackgroundService</c> dedicado a plano comercial (só a instalações/sync/alertas — ver
/// <c>backend/src/Nexora.Api.Cloud/Workers</c>), o caminho mínimo aceitável adotado é: a PRÓXIMA
/// leitura do plano (este handler) detecta o agendamento vencido e o efetiva antes de responder —
/// mesmo padrão já usado por <c>RestoreProductsPastBusinessDayCommandHandler</c> (E-08) para
/// "aplicar quando alguém olhar/rodar de novo". Idempotente porque <see cref="TenantPlanHistory.MarkApplied"/>
/// só roda quando a linha ainda está pendente (<see cref="TenantPlanHistory.IsPending"/>) — chamadas
/// concorrentes da mesma query não duplicam o evento (a segunda leitura já encontra a linha aplicada).
/// Isto é uma EXCEÇÃO deliberada à regra "query nunca escreve" (<c>TransactionBehavior</c> só roda
/// para <see cref="Nexora.Application.Abstractions.Messaging.ICommand"/>) — o SaveChangesAsync
/// abaixo só é chamado quando a efetivação de fato acontece.
///
/// A DIVERGÊNCIA entre capacidades efetivas (<c>tenant_config.plan_capabilities</c>) e o catálogo do
/// plano corrente É SÓ DETECTADA aqui (<c>consistent: false</c>) quando NENHUMA efetivação acontece
/// nesta chamada — nunca corrigida automaticamente (US-154 §10 "sem correção automática silenciosa");
/// a correção nesse caso é uma ação explícita do administrador via
/// <c>ReconcileTenantPlanConfigCommand</c>.
/// </summary>
internal sealed class GetTenantPlanQueryHandler
    : IRequestHandler<GetTenantPlanQuery, Result<TenantPlanResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantPlanQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantPlanResponse>> Handle(GetTenantPlanQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<TenantPlanResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        var tenantConfig = await _db.TenantConfigs
            .SingleOrDefaultAsync(c => c.TenantId == tenant.Id, cancellationToken);

        var pending = await _db.TenantPlanHistories
            .Where(h => h.TenantId == tenant.Id && h.AppliedAt == null)
            .OrderByDescending(h => h.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        TenantPlanScheduledResponse? scheduled = null;

        if (pending is not null && pending.EffectiveAt <= now)
        {
            // Efetivação preguiçosa/idempotente — ver docstring da classe.
            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var newPlanCatalogEntry = await _db.PlatformPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == pending.NextPlan, cancellationToken);

            var previousPlan = tenant.ApplyScheduledPlan(pending.NextPlan, now);

            var planChangedEvent = DomainEvent.Create(
                tenant.Id,
                type: "tenant.plan_changed",
                aggregateType: "tenant",
                aggregateId: tenant.Id,
                payload: JsonSerializer.Serialize(new
                {
                    tenantId = tenant.Id,
                    previousPlan,
                    plan = pending.NextPlan,
                    effectiveAt = pending.EffectiveAt,
                    actorId = pending.ActorId,
                }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: pending.ActorId);
            _db.DomainEvents.Add(planChangedEvent);

            pending.MarkApplied(now, planChangedEvent.Id);

            _db.AuditLogs.Add(AuditLog.Create(
                tenant.Id,
                action: "TENANT_PLAN_CHANGED",
                entity: "tenant",
                occurredAt: now,
                actorId: pending.ActorId,
                entityId: tenant.Id,
                before: JsonSerializer.Serialize(new { plan = previousPlan }),
                after: JsonSerializer.Serialize(new { plan = pending.NextPlan }),
                reason: pending.Reason,
                domainEventId: planChangedEvent.Id));

            // EVT-054: a efetivação do plano reconcilia as capacidades efetivas na mesma operação —
            // mesmo raciocínio de uma mudança IMEDIATA (UpdateTenantPlanCommandHandler); a
            // "divergência" que este handler apenas relata (sem corrigir) é outra situação: quando
            // NENHUMA efetivação acontece nesta chamada mas o estado já estava fora de sincronia.
            if (newPlanCatalogEntry is not null && tenantConfig is not null)
            {
                tenantConfig.ApplyPlanCapabilities(newPlanCatalogEntry.CapabilitiesJson, newPlanCatalogEntry.Version);

                _db.DomainEvents.Add(DomainEvent.Create(
                    tenant.Id,
                    type: "tenant.config_updated",
                    aggregateType: "tenant",
                    aggregateId: tenant.Id,
                    payload: JsonSerializer.Serialize(new { configVersion = tenantConfig.ConfigVersion, source = "PLAN" }),
                    origin: "CLOUD",
                    occurredAt: now,
                    actorId: pending.ActorId));
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (pending is not null)
        {
            scheduled = new TenantPlanScheduledResponse(pending.NextPlan, pending.EffectiveAt);
        }

        var platformPlan = await _db.PlatformPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == tenant.Plan, cancellationToken);

        var effectiveCapabilities = DeserializeCapabilities(tenantConfig?.PlanCapabilitiesJson);
        var catalogCapabilities = DeserializeCapabilities(platformPlan?.CapabilitiesJson);
        var consistent = platformPlan is not null
            && tenantConfig is not null
            && CapabilitySetsEqual(effectiveCapabilities, catalogCapabilities);

        var response = new TenantPlanResponse(
            tenant.Plan,
            effectiveCapabilities,
            scheduled,
            consistent,
            tenant.PlanVersion);

        return Result<TenantPlanResponse>.Success(response);
    }

    private static IReadOnlyList<string> DeserializeCapabilities(string? capabilitiesJson)
    {
        if (string.IsNullOrWhiteSpace(capabilitiesJson))
            return Array.Empty<string>();

        return JsonSerializer.Deserialize<List<string>>(capabilitiesJson) ?? new List<string>();
    }

    private static bool CapabilitySetsEqual(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
        a.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(b);
}
