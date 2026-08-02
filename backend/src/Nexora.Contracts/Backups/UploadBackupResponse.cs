namespace Nexora.Contracts.Backups;

public sealed record UploadBackupResponse(string Key, int Bytes, string Sha256);
