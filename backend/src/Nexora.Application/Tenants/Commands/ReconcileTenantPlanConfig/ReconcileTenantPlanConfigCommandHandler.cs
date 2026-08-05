using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.ReconcileTenantPlanConfig;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — corrige explicitamente a divergência entre
/// <c>tenant_config.plan_capabilities</c> e o catálogo do plano corrente do tenant. IDEMPOTENTE:
/// se já estiver consistente, não escreve nada e devolve <c>changed: false</c> — chamar duas vezes
/// seguidas produz o mesmo resultado observável, nunca um segundo evento/audit_log. AUDITADA
/// (RN-004): toda correção real grava <see cref="AuditLog"/> com autor e snapshot anterior/novo, e
/// emite <c>tenant.config_updated</c> (EVT-054, <c>source: "PLAN"</c>) na mesma transação.
/// </summary>
internal sealed class ReconcileTenantPlanConfigCommandHandler
    : IRequestHandler<ReconcileTenantPlanConfigCommand, Result<TenantPlanReconcileResponse>>
{
    private readonly IApplicationDbContext _db;

    public ReconcileTenantPlanConfigCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantPlanReconcileResponse>> Handle(
        ReconcileTenantPlanConfigCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<TenantPlanReconcileResponse>.Failure(
                "Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        var tenantConfig = await _db.TenantConfigs
            .SingleOrDefaultAsync(c => c.TenantId == tenant.Id, cancellationToken);

        if (tenantConfig is null)
        {
            return Result<TenantPlanReconcileResponse>.Failure(
                "Configuração do estabelecimento não encontrada.", ApiErrorCodes.TenantNotFound);
        }

        var platformPlan = await _db.PlatformPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == tenant.Plan, cancellationToken);

        if (platformPlan is null)
        {
            return Result<TenantPlanReconcileResponse>.Failure(
                "Plano comercial não encontrado no catálogo.", ApiErrorCodes.PlanNotAvailable);
        }

        var beforeCapabilities = tenantConfig.PlanCapabilitiesJson;
        var alreadyConsistent = tenantConfig.AppliedPlanVersion == platformPlan.Version
            && CapabilitySetsEqual(beforeCapabilities, platformPlan.CapabilitiesJson);

        if (alreadyConsistent)
        {
            return Result<TenantPlanReconcileResponse>.Success(new TenantPlanReconcileResponse(
                tenant.Plan,
                DeserializeCapabilities(beforeCapabilities),
                Consistent: true,
                Changed: false));
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        tenantConfig.ApplyPlanCapabilities(platformPlan.CapabilitiesJson, platformPlan.Version);

        var now = DateTimeOffset.UtcNow;
        var configUpdatedEvent = DomainEvent.Create(
            tenant.Id,
            type: "tenant.config_updated",
            aggregateType: "tenant",
            aggregateId: tenant.Id,
            payload: JsonSerializer.Serialize(new { configVersion = tenantConfig.ConfigVersion, source = "PLAN" }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId);
        _db.DomainEvents.Add(configUpdatedEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            tenant.Id,
            action: "TENANT_PLAN_CONFIG_RECONCILED",
            entity: "tenant_config",
            occurredAt: now,
            actorId: request.ActorId,
            entityId: tenant.Id,
            before: JsonSerializer.Serialize(new { capabilities = DeserializeCapabilities(beforeCapabilities) }),
            after: JsonSerializer.Serialize(new { capabilities = DeserializeCapabilities(platformPlan.CapabilitiesJson) }),
            reason: "Reconciliação de divergência entre plano comercial e configuração efetiva.",
            domainEventId: configUpdatedEvent.Id));

        return Result<TenantPlanReconcileResponse>.Success(new TenantPlanReconcileResponse(
            tenant.Plan,
            DeserializeCapabilities(platformPlan.CapabilitiesJson),
            Consistent: true,
            Changed: true));
    }

    private static IReadOnlyList<string> DeserializeCapabilities(string? capabilitiesJson)
    {
        if (string.IsNullOrWhiteSpace(capabilitiesJson))
            return Array.Empty<string>();

        return JsonSerializer.Deserialize<List<string>>(capabilitiesJson) ?? new List<string>();
    }

    private static bool CapabilitySetsEqual(string? a, string? b) =>
        DeserializeCapabilities(a).ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(DeserializeCapabilities(b));
}
