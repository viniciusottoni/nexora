using Nexora.Application.Abstractions.Platform;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de <see cref="IEdgeUpdateExecutor"/> para os testes de integração da US-146 — controla
/// deterministicamente em qual passo o ciclo de atualização falha (backup/download/migration/
/// health check), sem depender de infraestrutura real de container/Docker (que
/// <c>SimulatedEdgeUpdateExecutor</c> também não teria, mas por motivo diferente — ver docstring
/// daquela classe). Grava as chamadas para o teste afirmar que <c>RollbackAsync</c> foi
/// efetivamente disparado.
/// </summary>
internal sealed class FakeEdgeUpdateExecutor : IEdgeUpdateExecutor
{
    public bool BackupSucceeds { get; set; } = true;
    public bool DownloadSucceeds { get; set; } = true;
    public bool MigrationSucceeds { get; set; } = true;
    public bool HealthCheckSucceeds { get; set; } = true;

    public int RollbackCallCount { get; private set; }
    public string? LastRollbackPreviousVersion { get; private set; }

    public Task<EdgeBackupResult> BackupAsync(Guid tenantId, Guid installationId, string currentVersion, CancellationToken cancellationToken) =>
        Task.FromResult(BackupSucceeds
            ? new EdgeBackupResult(true, "fake-key", "fake-sha256", null)
            : new EdgeBackupResult(false, null, null, "backup simulado configurado para falhar"));

    public Task<EdgeDownloadResult> DownloadAsync(string targetVersion, CancellationToken cancellationToken) =>
        Task.FromResult(new EdgeDownloadResult(DownloadSucceeds, DownloadSucceeds ? null : "download simulado configurado para falhar"));

    public Task<EdgeMigrationResult> ApplyMigrationAsync(string targetVersion, CancellationToken cancellationToken) =>
        Task.FromResult(new EdgeMigrationResult(MigrationSucceeds, MigrationSucceeds ? null : "migration simulada configurada para falhar"));

    public Task<EdgeHealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken) =>
        Task.FromResult(HealthCheckSucceeds
            ? new EdgeHealthCheckResult(true, "OK", "OK", null)
            : new EdgeHealthCheckResult(false, "OK", "DOWN", "health check simulado configurado para falhar"));

    public Task RollbackAsync(Guid tenantId, Guid installationId, string previousVersion, CancellationToken cancellationToken)
    {
        RollbackCallCount++;
        LastRollbackPreviousVersion = previousVersion;
        return Task.CompletedTask;
    }
}
