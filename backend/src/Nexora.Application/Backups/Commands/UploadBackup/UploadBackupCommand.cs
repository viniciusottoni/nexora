using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Backups;

namespace Nexora.Application.Backups.Commands.UploadBackup;

/// <summary>
/// Recebe o dump de backup periódico enviado por um servidor edge (contingência de falha local).
/// Porta de <c>PUT /v1/platform/installations/:installationId/backups</c> (rota autenticada por
/// token de instalação, não por JWT de usuário).
/// </summary>
public sealed record UploadBackupCommand(
    Guid InstallationId,
    Guid AuthenticatedInstallationId,
    string BackupClass,
    string ExpectedSha256,
    byte[] Content) : ICommand<UploadBackupResponse>;
