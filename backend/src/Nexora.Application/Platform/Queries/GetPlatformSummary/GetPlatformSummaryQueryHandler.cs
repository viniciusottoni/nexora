using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Platform.Queries.GetPlatformSummary;

/// <summary>
/// US-150 §7/§8 — agrega contadores de <c>tenant</c>, <c>edge_installation</c> e <c>owner_invite</c>,
/// sem nenhuma linha individual (RN-015: o shell da plataforma não é onde dado operacional aparece).
/// <c>tenant</c> é a única tabela sem RLS (raiz global — mesmo raciocínio de
/// <see cref="Installations.Queries.Platform.ListPlatformInstallations.ListPlatformInstallationsQueryHandler"/>),
/// mas <c>edge_installation</c>/<c>owner_invite</c> exigem <c>app.tenant_id</c> fixado — por isso o
/// mesmo padrão de "itera tenant por tenant, fixando o contexto antes de cada leitura tenant-scoped".
/// </summary>
internal sealed class GetPlatformSummaryQueryHandler : IRequestHandler<GetPlatformSummaryQuery, Result<PlatformSummaryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetPlatformSummaryQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PlatformSummaryResponse>> Handle(GetPlatformSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var tenants = await _db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.Id, t.Status })
            .ToListAsync(cancellationToken);

        var tenantsTotal = tenants.Count;
        var tenantsActive = tenants.Count(t => t.Status == TenantStatus.Active);

        // "Atenção" é o estado que exige ação do administrador da plataforma — suspenso por
        // divergência comercial/operacional (RF-PLT-12, US-153). Trial e Cancelled não entram: o
        // primeiro é fluxo normal de implantação, o segundo já está encerrado (nada a fazer).
        var tenantsAttention = tenants.Count(t => t.Status == TenantStatus.Suspended);

        var installationsHealthy = 0;
        var installationsDegraded = 0;
        var installationsOffline = 0;
        var pendingInvites = 0;

        foreach (var tenant in tenants)
        {
            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var installations = await _db.EdgeInstallations.AsNoTracking()
                .Where(i => i.TenantId == tenant.Id && i.InstalledAt != null)
                .Select(i => i.LastSeenAt)
                .ToListAsync(cancellationToken);

            foreach (var lastSeenAt in installations)
            {
                switch (InstallationHealthClassifier.Classify(now, lastSeenAt))
                {
                    case InstallationHealthStatus.Ok:
                        installationsHealthy++;
                        break;
                    case InstallationHealthStatus.Degraded:
                        installationsDegraded++;
                        break;
                    default:
                        installationsOffline++;
                        break;
                }
            }

            pendingInvites += await _db.OwnerInvites.AsNoTracking()
                .CountAsync(
                    invite => invite.TenantId == tenant.Id && invite.ConsumedAt == null && invite.ExpiresAt > now,
                    cancellationToken);
        }

        var response = new PlatformSummaryResponse(
            new PlatformTenantsSummaryResponse(tenantsTotal, tenantsActive, tenantsAttention),
            new PlatformInstallationsSummaryResponse(installationsHealthy, installationsDegraded, installationsOffline),
            pendingInvites,
            now);

        return Result<PlatformSummaryResponse>.Success(response);
    }
}
