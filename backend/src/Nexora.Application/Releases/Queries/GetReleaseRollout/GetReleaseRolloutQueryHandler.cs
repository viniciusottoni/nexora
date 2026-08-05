using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Releases.Queries.GetReleaseRollout;

/// <summary>
/// US-146 §7/§10 — conta, para TODO o parque (todos os tenants), quantas instalações estão dentro
/// do SUBCONJUNTO elegível desta release (<see cref="Release.IsEligibleFor"/>) e em que estado cada
/// uma está. Mesmo padrão de varredura tenant-por-tenant de
/// <c>ListPlatformInstallationsQueryHandler</c>/<c>InstallationHealthEvaluationWorker</c> (ver
/// docstring de <c>PlatformInstallationLookup</c>): <c>tenant</c> é a única tabela sem RLS,
/// <c>edge_installation</c> exige <c>app.tenant_id</c> fixado por vez.
/// </summary>
/// <remarks>
/// <c>Total</c> é o tamanho do SUBCONJUNTO elegível (rollout gradual), não o parque inteiro — com
/// <c>rolloutPercent=10</c>, só ~10% das instalações contam aqui, exatamente o que o cenário
/// Gherkin "Liberação gradual" pede ("deve atingir um subconjunto primeiro"). Dentro do
/// subconjunto: <c>Updated</c> é quem já tem <see cref="EdgeInstallation.Version"/> igual à release;
/// <c>Failed</c> é quem ainda aponta <see cref="EdgeInstallation.TargetVersion"/> para esta release
/// mas a ÚLTIMA tentativa terminou em <see cref="EdgeUpdateStatus.Failed"/>/
/// <see cref="EdgeUpdateStatus.RolledBack"/> (escopado à versão-alvo corrente para não contar uma
/// falha antiga de uma release anterior já superada); <c>Pending</c> é o resto (ainda não tentou,
/// ou está <see cref="EdgeUpdateStatus.Deferred"/>/<see cref="EdgeUpdateStatus.InProgress"/>).
/// </remarks>
internal sealed class GetReleaseRolloutQueryHandler : IRequestHandler<GetReleaseRolloutQuery, Result<ReleaseRolloutResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetReleaseRolloutQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ReleaseRolloutResponse>> Handle(GetReleaseRolloutQuery request, CancellationToken cancellationToken)
    {
        var release = await _db.Releases.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Version == request.Version, cancellationToken);

        if (release is null)
        {
            return Result<ReleaseRolloutResponse>.Failure("Release não encontrada.", ApiErrorCodes.ReleaseNotFound);
        }

        var tenantIds = await _db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var updated = 0;
        var failed = 0;
        var pending = 0;

        foreach (var tenantId in tenantIds)
        {
            // RLS (ADR-004): edge_installation exige app.tenant_id fixado — sem isto a leitura
            // abaixo não retorna nenhuma linha (falha fechada).
            await _db.SetTenantContextAsync(tenantId, cancellationToken);

            var installations = await _db.EdgeInstallations.AsNoTracking()
                .Where(i => i.TenantId == tenantId && i.InstalledAt != null)
                .Select(i => new { i.Id, i.Version, i.TargetVersion, i.LastUpdateStatus })
                .ToListAsync(cancellationToken);

            foreach (var installation in installations)
            {
                if (!release.IsEligibleFor(installation.Id))
                {
                    continue;
                }

                if (installation.Version == release.Version)
                {
                    updated++;
                }
                else if (installation.TargetVersion == release.Version &&
                         (installation.LastUpdateStatus == nameof(EdgeUpdateStatus.Failed) ||
                          installation.LastUpdateStatus == nameof(EdgeUpdateStatus.RolledBack)))
                {
                    failed++;
                }
                else
                {
                    pending++;
                }
            }
        }

        var total = updated + failed + pending;

        return Result<ReleaseRolloutResponse>.Success(new ReleaseRolloutResponse(total, updated, failed, pending));
    }
}
