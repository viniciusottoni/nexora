using System.Security.Cryptography;
using Nexora.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Storage;

/// <summary>
/// Grava o dump de backup do edge no sistema de arquivos local do cloud, com poda por
/// retenção (30 dias para "six-hour", 90 dias para "daily"). Porta de
/// <c>filesystem-backup.storage.ts</c> do NestJS original — contingência de falha do servidor
/// local (pendência aberta no pacote de especificação sobre o destino definitivo do backup).
/// </summary>
public sealed class FileSystemBackupStorage : IBackupStorage
{
    private readonly FileSystemBackupStorageOptions _options;

    public FileSystemBackupStorage(IOptions<FileSystemBackupStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<BackupStored> PutAsync(BackupPutRequest request, CancellationToken cancellationToken = default)
    {
        var backupClassSegment = request.BackupClass == BackupClass.Daily ? "daily" : "six-hour";
        var directory = Path.Combine(_options.RootDirectory, request.TenantId.ToString(), request.InstallationId.ToString(), backupClassSegment);
        Directory.CreateDirectory(directory);

        var sha256 = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
        var fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH-mm-ss}-{sha256}.dump.enc";
        var target = Path.Combine(directory, fileName);
        var temp = $"{target}.tmp";

        await File.WriteAllBytesAsync(temp, request.Content, cancellationToken);
        File.Move(temp, target, overwrite: false);

        Prune(directory, backupClassSegment);

        var key = $"{request.TenantId}/{request.InstallationId}/{backupClassSegment}/{fileName}";
        return new BackupStored(key, request.Content.Length, sha256);
    }

    private static void Prune(string directory, string backupClassSegment)
    {
        var retentionDays = backupClassSegment == "daily" ? 90 : 30;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        foreach (var file in Directory.EnumerateFiles(directory, "*.dump.enc"))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff.UtcDateTime)
                File.Delete(file);
        }
    }
}
