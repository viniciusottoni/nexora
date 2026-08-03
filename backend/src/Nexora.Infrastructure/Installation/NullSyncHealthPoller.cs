using Nexora.Application.Installation.Abstractions;

namespace Nexora.Infrastructure.Installation;

/// <summary>
/// Implementação no-op de <see cref="ISyncHealthPoller"/> para hosts que não são o poller (a nuvem
/// é o alvo checado por <c>GET /v1/sync/health</c>, não quem chama — só o edge's SyncOutboxWorker
/// invoca <c>PollSyncHealthCommand</c>, ADR-039 exige que o handler ainda assim resolva a porta
/// neste host). Mesmo padrão de <c>NullBootstrapCatalogImporter</c>.
/// </summary>
public sealed class NullSyncHealthPoller : ISyncHealthPoller
{
    public Task<SyncHealthPollResult> PollAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new SyncHealthPollResult(DependencyStatus.Unknown, null));
}
