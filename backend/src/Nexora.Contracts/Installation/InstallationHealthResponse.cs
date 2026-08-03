namespace Nexora.Contracts.Installation;

/// <summary>
/// GET /v1/health (edge, endpoint público) — espelha <c>InstallationHealth</c> do probe original
/// em TypeScript. Status como string ("OK"/"DEGRADED"/"DOWN"/"UNKNOWN") para não depender de
/// configuração global de serialização de enum.
///
/// <see cref="OfflineSince"/> (US-034 §7) — não nulo desde quando a nuvem foi vista inalcançável
/// pela última vez até o exato heartbeat que detecta a reconexão (espelha
/// <c>EdgeInstallation.OfflineSince</c>); <c>null</c> quando a nuvem está alcançável ou o estado
/// de conectividade ainda é desconhecido (nenhum poll real ainda).
/// </summary>
public sealed record InstallationHealthResponse(
    string Postgres,
    string Redis,
    string Sync,
    int PendingEvents,
    DateTimeOffset? LastSyncAt,
    DateTimeOffset? OfflineSince,
    string Version);
