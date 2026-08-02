namespace Nexora.Application.Abstractions.Storage;

/// <summary>Classe de retenção do backup enviado pelo edge (ADR de contingência — 6h ou diário).</summary>
public enum BackupClass
{
    SixHour,
    Daily
}

public sealed record BackupPutRequest(
    Guid TenantId,
    Guid InstallationId,
    BackupClass BackupClass,
    byte[] Content);

public sealed record BackupStored(string Key, int Bytes, string Sha256);

/// <summary>
/// Armazenamento do dump de backup enviado periodicamente por cada servidor edge — implementado
/// em Infrastructure sobre o sistema de arquivos local do cloud (com poda por retenção). Porta de
/// <c>filesystem-backup.storage.ts</c> do NestJS original.
/// </summary>
public interface IBackupStorage
{
    Task<BackupStored> PutAsync(BackupPutRequest request, CancellationToken cancellationToken = default);
}
