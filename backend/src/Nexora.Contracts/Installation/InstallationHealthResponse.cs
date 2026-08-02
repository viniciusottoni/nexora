namespace Nexora.Contracts.Installation;

/// <summary>
/// GET /v1/health (edge, endpoint público) — espelha <c>InstallationHealth</c> do probe original
/// em TypeScript. Status como string ("OK"/"DEGRADED"/"DOWN"/"UNKNOWN") para não depender de
/// configuração global de serialização de enum.
/// </summary>
public sealed record InstallationHealthResponse(
    string Postgres,
    string Redis,
    string Sync,
    int PendingEvents,
    DateTimeOffset? LastSyncAt,
    string Version);
