namespace Nexora.Application.Abstractions.Platform;

public sealed record EdgeBackupResult(bool Succeeded, string? BackupKey, string? Sha256, string? FailureReason);

public sealed record EdgeDownloadResult(bool Succeeded, string? FailureReason);

public sealed record EdgeMigrationResult(bool Succeeded, string? FailureReason);

public sealed record EdgeHealthCheckResult(bool Succeeded, string Postgres, string Redis, string? FailureReason);

/// <summary>
/// Porta dos passos técnicos do ciclo de atualização do edge (US-146 §7 pseudocódigo: "gera backup
/// → baixa imagens → aplica migration → health check → ativa ou reverte") — mesmo idioma de
/// <see cref="IDomainVerificationService"/>/<see cref="ICertificateIssuer"/> (US-143): a DECISÃO de
/// quando/se seguir cada passo é 100% testável em <c>Nexora.Application</c>
/// (<c>RunEdgeUpdateCycleCommandHandler</c>), a EXECUÇÃO de infraestrutura real (pull de imagem
/// Docker, dump/restore de banco, restart de container) fica atrás desta porta, implementada em
/// <c>Nexora.Infrastructure</c>. Ver <c>Nexora.Infrastructure.Platform.SimulatedEdgeUpdateExecutor</c>
/// para o que é genuinamente real hoje (backup e health check) versus simulado (download/migration/
/// rollback) — este sandbox não tem posse de um host Docker real para o edge, ao contrário do
/// Postgres/Redis de desenvolvimento, que existem de verdade.
/// </summary>
public interface IEdgeUpdateExecutor
{
    Task<EdgeBackupResult> BackupAsync(Guid tenantId, Guid installationId, string currentVersion, CancellationToken cancellationToken);

    Task<EdgeDownloadResult> DownloadAsync(string targetVersion, CancellationToken cancellationToken);

    Task<EdgeMigrationResult> ApplyMigrationAsync(string targetVersion, CancellationToken cancellationToken);

    Task<EdgeHealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reverte para a versão anterior após falha de health check (US-146 §4, cenário "Rollback
    /// automático") — no mundo real, restaura o backup gerado por <see cref="BackupAsync"/> e volta
    /// a subir os containers da versão anterior.
    /// </summary>
    Task RollbackAsync(Guid tenantId, Guid installationId, string previousVersion, CancellationToken cancellationToken);
}
