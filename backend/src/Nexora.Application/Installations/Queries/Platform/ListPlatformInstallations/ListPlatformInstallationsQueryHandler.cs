using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Installations.Support;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Queries.Platform.ListPlatformInstallations;

/// <summary>
/// US-140 §4 "Visão consolidada" — junta <c>EdgeInstallation</c> com <c>Tenant</c>/<c>Store</c> para
/// TODO o parque, sem filtro de tenant único (leitura de plataforma). <c>tenant</c> é a única
/// tabela de negócio sem RLS (ADR-004: "raiz da hierarquia"); <c>store</c>/<c>edge_installation</c>/
/// <c>outbox</c>(tenant_id, sem RLS própria)/<c>alert</c> exigem <c>app.tenant_id</c> fixado —
/// mesmo padrão de <c>AlertEvaluationWorker</c>: itera tenant por tenant, fixando o contexto antes
/// de cada leitura tenant-scoped (ver docstring de <see cref="PlatformInstallationLookup"/> para a
/// mesma decisão aplicada à busca por id único).
/// </summary>
internal sealed class ListPlatformInstallationsQueryHandler
    : IRequestHandler<ListPlatformInstallationsQuery, Result<PlatformInstallationListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAppVersionProvider _version;

    public ListPlatformInstallationsQueryHandler(IApplicationDbContext db, IAppVersionProvider version)
    {
        _db = db;
        _version = version;
    }

    public async Task<Result<PlatformInstallationListResponse>> Handle(
        ListPlatformInstallationsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // US-140 §7 "expectedVersion" — versão-alvo do parque hoje é a versão corrente do binário
        // da nuvem (releases/rollout-aware por instalação é US-146, fora do escopo desta história).
        var expectedVersion = _version.CurrentVersion;

        var tenants = await _db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        var summaries = new List<PlatformInstallationSummaryResponse>();

        foreach (var tenant in tenants)
        {
            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var installations = await _db.EdgeInstallations.AsNoTracking()
                .Where(i => i.TenantId == tenant.Id && i.InstalledAt != null)
                .ToListAsync(cancellationToken);

            if (installations.Count == 0)
            {
                continue;
            }

            var storeNames = await _db.Stores.AsNoTracking()
                .Where(s => s.TenantId == tenant.Id)
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            // outbox não tem RLS própria (edge, single-tenant por natureza — Docs/Domain/10 §2);
            // filtra por TenantId explicitamente porque o banco não filtra por nós aqui.
            var pendingEvents = await _db.OutboxEntries.AsNoTracking()
                .CountAsync(o => o.TenantId == tenant.Id && o.Status != "SYNCED", cancellationToken);

            var openAlerts = await _db.Alerts.AsNoTracking()
                .CountAsync(a => a.TenantId == tenant.Id && a.ResolvedAt == null, cancellationToken);

            foreach (var installation in installations)
            {
                var health = InstallationHealthClassifier.Classify(now, installation.LastSeenAt);
                var syncLagSeconds = installation.LastSeenAt is { } lastSeenAt
                    ? (long?)Math.Max(0, (long)(now - lastSeenAt).TotalSeconds)
                    : null;

                summaries.Add(new PlatformInstallationSummaryResponse(
                    InstallationId: installation.Id,
                    TenantId: tenant.Id,
                    TenantName: tenant.Name,
                    StoreName: storeNames.TryGetValue(installation.StoreId, out var storeName) ? storeName : "—",
                    Version: installation.Version,
                    ExpectedVersion: expectedVersion,
                    LastSeenAt: installation.LastSeenAt,
                    SyncLagSeconds: syncLagSeconds,
                    PendingEvents: pendingEvents,
                    OpenAlerts: openAlerts,
                    Health: health.ToWireLabel()));
            }
        }

        // US-140 §10 "ordenação por criticidade como padrão": DOWN, depois DEGRADED, depois OK;
        // dentro do mesmo nível, quem está sem contato há mais tempo aparece primeiro.
        var ordered = summaries
            .OrderByDescending(HealthRank)
            .ThenBy(s => s.LastSeenAt ?? DateTimeOffset.MinValue)
            .ToList();

        return Result<PlatformInstallationListResponse>.Success(new PlatformInstallationListResponse(ordered));
    }

    private static int HealthRank(PlatformInstallationSummaryResponse summary) => summary.Health switch
    {
        "DOWN" => 2,
        "DEGRADED" => 1,
        _ => 0
    };
}
