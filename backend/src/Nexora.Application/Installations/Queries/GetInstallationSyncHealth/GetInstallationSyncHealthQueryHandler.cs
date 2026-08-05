using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Contracts.Installations;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Queries.GetInstallationSyncHealth;

/// <summary>
/// US-146 §7 estende o campo <c>expectedVersion</c> deste endpoint (que já existia desde US-006)
/// para ser CONSCIENTE de liberação gradual: em vez de sempre devolver a versão corrente estática
/// do binário da nuvem (<see cref="IAppVersionProvider.CurrentVersion"/>), agora procura a
/// <see cref="Release"/> publicada de MAIOR <c>RolloutPercent</c> para a qual esta instalação é
/// elegível (<see cref="Release.IsEligibleFor"/> — bucketing determinístico e estável) e, se essa
/// versão for diferente da instalada, agenda a atualização (<see cref="EdgeInstallation.ScheduleUpdate"/>).
/// Sem nenhuma <see cref="Release"/> publicada (caso zero-releases, todo o comportamento anterior a
/// esta história), cai no fallback de sempre: <see cref="IAppVersionProvider.CurrentVersion"/> —
/// mantém <c>PollSyncHealthIntegrationTests</c>/testes existentes deste handler verdes.
/// </summary>
internal sealed class GetInstallationSyncHealthQueryHandler
    : IRequestHandler<GetInstallationSyncHealthQuery, Result<InstallationSyncHealthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAppVersionProvider _version;

    public GetInstallationSyncHealthQueryHandler(IApplicationDbContext db, IAppVersionProvider version)
    {
        _db = db;
        _version = version;
    }

    public async Task<Result<InstallationSyncHealthResponse>> Handle(
        GetInstallationSyncHealthQuery request, CancellationToken cancellationToken)
    {
        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);

        if (config is null)
        {
            return Result<InstallationSyncHealthResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        var expectedVersion = await ResolveExpectedVersionAsync(request.TenantId, request.InstallationId, cancellationToken);

        return Result<InstallationSyncHealthResponse>.Success(new InstallationSyncHealthResponse(
            DateTimeOffset.UtcNow, expectedVersion, config.ConfigVersion));
    }

    /// <summary>
    /// Exceção DELIBERADA e documentada à convenção "query nunca escreve" (ver
    /// <c>Nexora.Application.Abstractions.Behaviors.TransactionBehavior</c>, que só chama
    /// <c>SaveChangesAsync</c> para <see cref="Nexora.Application.Abstractions.Messaging.ICommand"/>):
    /// este é o único ponto em que a nuvem "descobre" que uma instalação entrou no subconjunto
    /// elegível de uma release nova — é exatamente o momento do PRÓPRIO poll de saúde periódico
    /// (US-006/US-034, <c>SyncOutboxWorker</c>) que precisa registrar isso, e adiar para um comando
    /// separado obrigaria a instalação a fazer uma segunda viagem de rede só para "confirmar" o que
    /// este GET já calculou. Não emite <c>DomainEvent</c> nenhum (US-146 §6: "Não se aplica a esta
    /// história") — <see cref="EdgeInstallation.TargetVersion"/> é metadado operacional de
    /// rollout, não uma transição de negócio (ADR-006 não se aplica).
    /// </summary>
    private async Task<string> ResolveExpectedVersionAsync(Guid tenantId, Guid installationId, CancellationToken cancellationToken)
    {
        var candidateReleases = await _db.Releases.AsNoTracking()
            .Where(r => r.RolloutPercent > 0)
            .OrderByDescending(r => r.RolloutPercent)
            .ThenByDescending(r => r.PublishedAt)
            .ToListAsync(cancellationToken);

        if (candidateReleases.Count == 0)
        {
            // Fallback — nenhuma Release publicada ainda (comportamento pré-US-146).
            return _version.CurrentVersion;
        }

        var eligibleRelease = candidateReleases.FirstOrDefault(r => r.IsEligibleFor(installationId));
        if (eligibleRelease is null)
        {
            return _version.CurrentVersion;
        }

        var installation = await _db.EdgeInstallations
            .FirstOrDefaultAsync(i => i.Id == installationId && i.TenantId == tenantId, cancellationToken);
        if (installation is not null &&
            installation.Version != eligibleRelease.Version &&
            installation.TargetVersion != eligibleRelease.Version)
        {
            installation.ScheduleUpdate(eligibleRelease.Version);

            // Ver docstring do método — exceção deliberada, TransactionBehavior não persiste
            // escrita de IQuery.
            await _db.SaveChangesAsync(cancellationToken);
        }

        return eligibleRelease.Version;
    }
}
