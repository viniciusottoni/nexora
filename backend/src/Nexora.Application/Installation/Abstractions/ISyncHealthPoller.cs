namespace Nexora.Application.Installation.Abstractions;

/// <summary>Status de dependência — mesmo vocabulário do probe original (DependencyStatus em TS).</summary>
public enum DependencyStatus
{
    Ok,
    Degraded,
    Down,
    Unknown
}

public sealed record SyncHealthPollResult(DependencyStatus Status, int? HttpStatusCode);

/// <summary>
/// Porta técnica para checar a nuvem a partir do edge (GET /v1/sync/health, assinado — ver
/// <c>installation-request-auth.ts</c>). Implementada em Infrastructure com HttpClient +
/// serviço de assinatura de requisição (<c>IInstallationRequestSigner</c>).
/// Usada pelo <see cref="Commands.PollSyncHealth.PollSyncHealthCommandHandler"/>, disparado
/// periodicamente por <c>SyncOutboxWorker</c> (BackgroundService, ADR-037).
/// </summary>
public interface ISyncHealthPoller
{
    Task<SyncHealthPollResult> PollAsync(CancellationToken cancellationToken);
}
