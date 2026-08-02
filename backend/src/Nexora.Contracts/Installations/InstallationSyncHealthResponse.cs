namespace Nexora.Contracts.Installations;

/// <summary>GET /v1/sync/health (cloud, autenticado) — espelha o retorno do <c>InitialSyncController.health</c> original.</summary>
public sealed record InstallationSyncHealthResponse(
    DateTimeOffset ServerTime,
    string ExpectedVersion,
    int ConfigVersion);
