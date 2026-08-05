namespace Nexora.Contracts.Platform;

/// <summary>
/// Contratos do painel de instalações com saúde (US-140 §7). Vive em <c>Nexora.Contracts</c>
/// (só referencia <c>Nexora.Domain</c> — ADR-039) porque é consumido tanto pela Application
/// (handlers de query) quanto pela Api.Cloud (controller) e serializado como camelCase no fio via
/// <c>System.Text.Json</c> (default do ASP.NET Core).
/// </summary>
/// <remarks>
/// Uma linha da lista consolidada de instalações — <c>GET /v1/platform/installations</c>.
/// Nunca inclui dado de negócio do tenant (RN-015) — só identificação (nome do estabelecimento/
/// loja) e saúde técnica.
/// </remarks>
public sealed record PlatformInstallationSummaryResponse(
    Guid InstallationId,
    Guid TenantId,
    string TenantName,
    string StoreName,
    string? Version,
    string ExpectedVersion,
    DateTimeOffset? LastSeenAt,
    long? SyncLagSeconds,
    int PendingEvents,
    int OpenAlerts,
    string Health);

public sealed record PlatformInstallationListResponse(IReadOnlyList<PlatformInstallationSummaryResponse> Data);

/// <summary>
/// Health check remoto (US-140 §7, §10 "diagnóstico remoto"). <see cref="Postgres"/>/<see cref="Redis"/>/
/// <see cref="Sync"/> refletem o JSONB livre de <c>EdgeInstallation.Health</c> — qualquer sub-status
/// ausente no payload mais recente vem <c>null</c> (não inventa dado que a instalação não reportou).
/// </summary>
public sealed record InstallationHealthCheckResponse(string? Postgres, string? Redis, string? Sync, DateTimeOffset? CheckedAt);

/// <summary>
/// Linha de log recente — RN-015: nunca conteúdo de negócio, só evento técnico já capturado
/// (transição de conectividade, incidente aberto/fechado). Tailing de log remoto de verdade é um
/// follow-up de infraestrutura (ver docstring de <c>GetInstallationDiagnosticsQueryHandler</c>).
/// </summary>
public sealed record InstallationLogEntryResponse(DateTimeOffset OccurredAt, string Level, string Message);

public sealed record InstallationDiagnosticsResponse(
    InstallationHealthCheckResponse HealthCheck,
    IReadOnlyList<InstallationLogEntryResponse> RecentLogs,
    int? DiskUsagePercent,
    DateTimeOffset? LastBackupAt);

/// <summary>Uma linha do histórico de incidentes (US-140 §4, cenário "Histórico de incidentes" — duração e causa).</summary>
public sealed record InstallationIncidentResponse(
    Guid Id,
    string Type,
    DateTimeOffset StartedAt,
    DateTimeOffset? ResolvedAt,
    string? Cause,
    long? DurationSeconds);

public sealed record InstallationIncidentListResponse(IReadOnlyList<InstallationIncidentResponse> Data);
