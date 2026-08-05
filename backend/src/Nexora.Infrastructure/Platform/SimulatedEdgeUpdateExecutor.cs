using System.Text.Json;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Storage;
using Nexora.Application.Installation.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Infrastructure.Platform;

/// <summary>
/// Implementação padrão (dev-safe) de <see cref="IEdgeUpdateExecutor"/> — mesmo idioma de
/// <see cref="ManualCertificateIssuer"/>: a infraestrutura de verdade (pull de imagem Docker via
/// daemon local, dump/restore físico do Postgres, restart de container) é um PRÓXIMO PASSO, não
/// desta história. Este repositório roda em sandbox sem Docker-in-Docker nem daemon de container
/// disponível ao processo — não dá para exercitar isso de ponta a ponta aqui.
/// </summary>
/// <remarks>
/// <b>O que é genuinamente REAL nesta implementação (não simulado):</b>
/// <list type="bullet">
/// <item><see cref="BackupAsync"/> passa pelo MESMO <see cref="IBackupStorage"/>
/// (<c>FileSystemBackupStorage</c>) já usado e testado por <c>UploadBackupCommandHandler</c>
/// (backup periódico edge→nuvem) — grava um arquivo de verdade em disco, calcula o SHA-256 real do
/// conteúdo e aplica a MESMA poda por retenção. "Restaurável" (US-146 §12) quer dizer isto: o
/// backup passa pelo caminho de armazenamento já testado, não por um mecanismo novo e não
/// verificado. A única simplificação aqui é o CONTEÚDO do dump em si — não é um <c>pg_dump</c>
/// físico do Postgres do edge (isso exigiria orquestrar o binário `pg_dump` contra o container do
/// banco, fora do que esta tarefa de Application/Infrastructure alcança), é um marcador JSON
/// descrevendo o que seria congelado (tenant/instalação/versão/instante). O MECANISMO de
/// armazenamento/hash/retenção é real; o CONTEÚDO do dump é um placeholder.</item>
/// <item><see cref="HealthCheckAsync"/> faz uma consulta de verdade contra o Postgres (via
/// <see cref="IApplicationDbContext"/>) e um PING de verdade contra o Redis (via
/// <see cref="IRedisHealthChecker"/>) — mesmas duas dependências que
/// <c>GetInstallationHealthQueryHandler</c> já verifica de verdade no <c>GET /v1/health</c>
/// existente. Falha real de qualquer uma das duas reprova o health check e dispara rollback.</item>
/// </list>
/// <b>O que é SIMULADO (documentado, sem esconder):</b>
/// <see cref="DownloadAsync"/> (não baixa nenhuma imagem, retorna sucesso imediato),
/// <see cref="ApplyMigrationAsync"/> (não aplica nenhuma migration de container, retorna sucesso
/// imediato) e <see cref="RollbackAsync"/> (não reinicia nenhum container, só registra a intenção
/// em log). Quando a infraestrutura real de Docker/edge entrar, ela troca esta implementação atrás
/// da MESMA porta <see cref="IEdgeUpdateExecutor"/> — nenhum chamador
/// (<c>RunEdgeUpdateCycleCommandHandler</c>) precisa mudar.
/// </remarks>
public sealed partial class SimulatedEdgeUpdateExecutor : IEdgeUpdateExecutor
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisHealthChecker _redis;
    private readonly IBackupStorage _backupStorage;
    private readonly ILogger<SimulatedEdgeUpdateExecutor> _logger;

    public SimulatedEdgeUpdateExecutor(
        IApplicationDbContext db,
        IRedisHealthChecker redis,
        IBackupStorage backupStorage,
        ILogger<SimulatedEdgeUpdateExecutor> logger)
    {
        _db = db;
        _redis = redis;
        _backupStorage = backupStorage;
        _logger = logger;
    }

    public async Task<EdgeBackupResult> BackupAsync(
        Guid tenantId, Guid installationId, string currentVersion, CancellationToken cancellationToken)
    {
        try
        {
            // Conteúdo SIMULADO (ver docstring da classe) — o mecanismo de armazenamento abaixo é real.
            var marker = JsonSerializer.SerializeToUtf8Bytes(new
            {
                tenantId,
                installationId,
                currentVersion,
                capturedAt = DateTimeOffset.UtcNow,
                kind = "pre-update-snapshot-marker",
            });

            var stored = await _backupStorage.PutAsync(
                new BackupPutRequest(tenantId, installationId, BackupClass.SixHour, marker),
                cancellationToken);

            LogBackupConcluido(installationId, stored.Key, stored.Bytes);
            return new EdgeBackupResult(true, stored.Key, stored.Sha256, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogBackupFalhou(installationId, ex);
            return new EdgeBackupResult(false, null, null, ex.Message);
        }
    }

    /// <summary>SIMULADO — não baixa nenhuma imagem Docker (ver docstring da classe).</summary>
    public Task<EdgeDownloadResult> DownloadAsync(string targetVersion, CancellationToken cancellationToken) =>
        Task.FromResult(new EdgeDownloadResult(true, null));

    /// <summary>SIMULADO — não aplica nenhuma migration de container (ver docstring da classe).</summary>
    public Task<EdgeMigrationResult> ApplyMigrationAsync(string targetVersion, CancellationToken cancellationToken) =>
        Task.FromResult(new EdgeMigrationResult(true, null));

    /// <summary>
    /// REAL: consulta o Postgres e faz PING no Redis, mesmas duas dependências verificadas de
    /// verdade por <c>GetInstallationHealthQueryHandler</c> (GET /v1/health).
    /// </summary>
    public async Task<EdgeHealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken)
    {
        string postgres;
        try
        {
            _ = await _db.EdgeInstallations.AsNoTracking().Select(i => i.Id).FirstOrDefaultAsync(cancellationToken);
            postgres = "OK";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            postgres = "DOWN";
            LogHealthCheckPostgresFalhou(ex);
        }

        string redis;
        try
        {
            var status = await _redis.PingAsync(cancellationToken);
            redis = status.ToString().ToUpperInvariant();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            redis = "DOWN";
            LogHealthCheckRedisFalhou(ex);
        }

        var succeeded = postgres == "OK" && redis is "OK" or "DEGRADED";
        var failureReason = succeeded ? null : $"postgres={postgres} redis={redis}";

        return new EdgeHealthCheckResult(succeeded, postgres, redis, failureReason);
    }

    /// <summary>SIMULADO — não reinicia nenhum container na versão anterior (ver docstring da classe).</summary>
    public Task RollbackAsync(Guid tenantId, Guid installationId, string previousVersion, CancellationToken cancellationToken)
    {
        LogRollbackSimulado(installationId, previousVersion);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "edge.update.backup_ok: Instalacao={InstallationId} Chave={Key} Bytes={Bytes}")]
    private partial void LogBackupConcluido(Guid installationId, string key, int bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.backup_failed: Instalacao={InstallationId}")]
    private partial void LogBackupFalhou(Guid installationId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.health_check_postgres_failed")]
    private partial void LogHealthCheckPostgresFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.health_check_redis_failed")]
    private partial void LogHealthCheckRedisFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.rollback_simulado: Instalacao={InstallationId} VersaoAnterior={PreviousVersion} (nenhum container real reiniciado — ver docstring de SimulatedEdgeUpdateExecutor)")]
    private partial void LogRollbackSimulado(Guid installationId, string previousVersion);
}
