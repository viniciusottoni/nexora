namespace Nexora.Infrastructure.Storage;

public sealed class FileSystemBackupStorageOptions
{
    public const string SectionName = "BackupStorage";

    /// <summary>Raiz onde os dumps de backup do edge são gravados — um diretório por tenant/instalação/classe.</summary>
    public string RootDirectory { get; set; } = "/var/lib/nexora-cloud/backups";
}
