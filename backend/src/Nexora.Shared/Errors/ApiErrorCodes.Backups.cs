namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de backup do edge (upload e alerta de falha — ADR-021).</summary>
public static partial class ApiErrorCodes
{
    /// <summary>O SHA-256 declarado no cabeçalho não confere com o conteúdo recebido nem com o que foi persistido.</summary>
    public const string BackupHashMismatch = "BACKUP_HASH_MISMATCH";

    /// <summary>O <c>installationId</c> da rota não é o mesmo autenticado pelo token de instalação (ADR-016/instalação).</summary>
    public const string BackupPermissionDenied = "BACKUP_PERMISSION_DENIED";
}
