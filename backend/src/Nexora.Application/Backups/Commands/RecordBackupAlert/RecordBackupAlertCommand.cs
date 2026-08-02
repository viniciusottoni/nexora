using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Backups.Commands.RecordBackupAlert;

/// <summary>
/// Registra que um servidor edge falhou ao enviar seu backup periódico. Porta de
/// <c>POST /v1/platform/installations/:installationId/backup-alerts</c>.
/// </summary>
public sealed record RecordBackupAlertCommand(
    Guid InstallationId,
    Guid AuthenticatedInstallationId,
    string Reason,
    DateTimeOffset OccurredAt) : ICommand;
