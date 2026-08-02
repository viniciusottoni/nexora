namespace Nexora.Contracts.Backups;

/// <summary>Corpo de <c>POST /v1/platform/installations/:installationId/backup-alerts</c>.</summary>
public sealed record RecordBackupAlertRequest(string Reason, DateTimeOffset OccurredAt);
