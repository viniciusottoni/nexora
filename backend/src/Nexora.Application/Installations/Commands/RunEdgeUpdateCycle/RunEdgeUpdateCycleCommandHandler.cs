using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Installations.Support;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installations.Commands.RunEdgeUpdateCycle;

/// <summary>
/// Orquestra o ciclo de atualização controlada do parque NO LADO EDGE (US-146 §7). Passos, na
/// ordem do pseudocódigo do doc.:
/// <list type="number">
/// <item>Versão esperada já foi resolvida por <c>GetInstallationSyncHealthQueryHandler</c> no poll
/// anterior — aqui só lê <see cref="EdgeInstallation.TargetVersion"/> já persistido.</item>
/// <item>Pendências de sincronização (<see cref="EdgeUpdateSyncPendingPolicy"/>) — acima do limiar,
/// adia (<see cref="EdgeUpdateStatus.Deferred"/>) e informa a plataforma.</item>
/// <item>Backup (<see cref="IEdgeUpdateExecutor.BackupAsync"/>, ADR-033 — antes de qualquer
/// migration).</item>
/// <item>Download das imagens (<see cref="IEdgeUpdateExecutor.DownloadAsync"/>).</item>
/// <item>Migration (<see cref="IEdgeUpdateExecutor.ApplyMigrationAsync"/>).</item>
/// <item>Health check (<see cref="IEdgeUpdateExecutor.HealthCheckAsync"/>) — sucesso ativa
/// (<see cref="EdgeUpdateStatus.Succeeded"/>); falha dispara
/// <see cref="IEdgeUpdateExecutor.RollbackAsync"/> automático
/// (<see cref="EdgeUpdateStatus.RolledBack"/>) e alerta a plataforma.</item>
/// </list>
/// ADR-019: só o próprio edge decide agir, dentro da janela que ELE calcula a partir da sua
/// <c>TenantConfig.Maintenance</c> — nada aqui é acionado pela nuvem.
/// </summary>
internal sealed partial class RunEdgeUpdateCycleCommandHandler : IRequestHandler<RunEdgeUpdateCycleCommand, Result<RunEdgeUpdateCycleResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEdgeUpdateExecutor _executor;
    private readonly IPlatformAlertNotifier _platformAlertNotifier;
    private readonly ILogger<RunEdgeUpdateCycleCommandHandler> _logger;

    public RunEdgeUpdateCycleCommandHandler(
        IApplicationDbContext db,
        IEdgeUpdateExecutor executor,
        IPlatformAlertNotifier platformAlertNotifier,
        ILogger<RunEdgeUpdateCycleCommandHandler> logger)
    {
        _db = db;
        _executor = executor;
        _platformAlertNotifier = platformAlertNotifier;
        _logger = logger;
    }

    public async Task<Result<RunEdgeUpdateCycleResult>> Handle(RunEdgeUpdateCycleCommand request, CancellationToken cancellationToken)
    {
        var installation = await _db.EdgeInstallations.FirstOrDefaultAsync(cancellationToken);
        if (installation is null)
        {
            // Edge ainda não passou pelo bootstrap (ImportBootstrapCommand) — nada a fazer, mesmo
            // guard de PollSyncHealthCommandHandler.
            LogSemInstalacao();
            return Result<RunEdgeUpdateCycleResult>.Success(new RunEdgeUpdateCycleResult("NoInstallation", null));
        }

        var targetVersion = installation.TargetVersion;
        if (string.IsNullOrWhiteSpace(targetVersion) || targetVersion == installation.Version)
        {
            // Já em dia — a próxima versão elegível só chega quando o poll de saúde seguinte
            // (GetInstallationSyncHealthQueryHandler) descobrir uma release nova.
            return Result<RunEdgeUpdateCycleResult>.Success(new RunEdgeUpdateCycleResult("NoUpdatePending", null));
        }

        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == installation.TenantId, cancellationToken);

        var (startHourUtc, endHourUtc) = EdgeUpdateWindowPolicy.ResolveWindow(config?.Maintenance);
        if (!EdgeUpdateWindowPolicy.IsWithinWindow(DateTimeOffset.UtcNow, startHourUtc, endHourUtc))
        {
            return Result<RunEdgeUpdateCycleResult>.Success(new RunEdgeUpdateCycleResult("OutsideWindow", null));
        }

        // Outbox não tem RLS própria (edge, single-tenant por natureza — Docs/Domain/10 §2); em
        // produção o Postgres do edge só tem linhas do próprio tenant, então filtrar por
        // TenantId aqui é um no-op (mesmo resultado de PollSyncHealthCommandHandler, que não
        // filtra). Filtro explícito mantido de propósito — mesma convenção de
        // ListPlatformInstallationsQueryHandler, que também precisa filtrar manualmente por não
        // haver RLS nesta tabela.
        var pendingEvents = await _db.OutboxEntries.AsNoTracking()
            .CountAsync(o => o.TenantId == installation.TenantId && o.Status != "SYNCED", cancellationToken);

        if (EdgeUpdateSyncPendingPolicy.ShouldDefer(pendingEvents))
        {
            var deferredAt = DateTimeOffset.UtcNow;
            installation.RecordUpdateResult(EdgeUpdateStatus.Deferred, deferredAt);
            LogAtualizacaoAdiada(installation.Id, targetVersion, pendingEvents);

            await _platformAlertNotifier.NotifyEdgeUpdateDeferredAsync(
                installation.TenantId, installation.Id, targetVersion, pendingEvents, cancellationToken);

            // SaveChangesAsync é feito pelo TransactionBehavior (command).
            return Result<RunEdgeUpdateCycleResult>.Success(
                new RunEdgeUpdateCycleResult(nameof(EdgeUpdateStatus.Deferred), $"{pendingEvents} eventos pendentes"));
        }

        var previousVersion = installation.Version ?? "0.0.0";

        var backup = await _executor.BackupAsync(installation.TenantId, installation.Id, previousVersion, cancellationToken);
        if (!backup.Succeeded)
        {
            var failedAt = DateTimeOffset.UtcNow;
            installation.RecordUpdateResult(EdgeUpdateStatus.Failed, failedAt);
            LogPassoFalhou(installation.Id, "backup", backup.FailureReason);
            return Result<RunEdgeUpdateCycleResult>.Success(
                new RunEdgeUpdateCycleResult(nameof(EdgeUpdateStatus.Failed), $"Backup falhou: {backup.FailureReason}"));
        }

        var download = await _executor.DownloadAsync(targetVersion, cancellationToken);
        if (!download.Succeeded)
        {
            installation.RecordUpdateResult(EdgeUpdateStatus.Failed, DateTimeOffset.UtcNow);
            LogPassoFalhou(installation.Id, "download", download.FailureReason);
            return Result<RunEdgeUpdateCycleResult>.Success(
                new RunEdgeUpdateCycleResult(nameof(EdgeUpdateStatus.Failed), $"Download falhou: {download.FailureReason}"));
        }

        var migration = await _executor.ApplyMigrationAsync(targetVersion, cancellationToken);
        if (!migration.Succeeded)
        {
            installation.RecordUpdateResult(EdgeUpdateStatus.Failed, DateTimeOffset.UtcNow);
            LogPassoFalhou(installation.Id, "migration", migration.FailureReason);
            return Result<RunEdgeUpdateCycleResult>.Success(
                new RunEdgeUpdateCycleResult(nameof(EdgeUpdateStatus.Failed), $"Migration falhou: {migration.FailureReason}"));
        }

        var health = await _executor.HealthCheckAsync(cancellationToken);
        var completedAt = DateTimeOffset.UtcNow;

        if (health.Succeeded)
        {
            installation.RecordUpdateResult(EdgeUpdateStatus.Succeeded, completedAt, targetVersion);
            LogAtualizacaoConcluida(installation.Id, targetVersion);

            // SaveChangesAsync é feito pelo TransactionBehavior (command).
            return Result<RunEdgeUpdateCycleResult>.Success(new RunEdgeUpdateCycleResult(nameof(EdgeUpdateStatus.Succeeded), targetVersion));
        }

        // Cenário Gherkin "Rollback automático" (US-146 §4): health check pós-migration falhou —
        // reverte para a versão anterior e alerta a plataforma. O backup gerado acima é a garantia
        // de que a reversão tem o que restaurar (ADR-033).
        await _executor.RollbackAsync(installation.TenantId, installation.Id, previousVersion, cancellationToken);
        installation.RecordUpdateResult(EdgeUpdateStatus.RolledBack, completedAt);
        LogRollback(installation.Id, targetVersion, previousVersion, health.FailureReason);

        await _platformAlertNotifier.NotifyEdgeUpdateRolledBackAsync(
            installation.TenantId, installation.Id, targetVersion, previousVersion, health.FailureReason, cancellationToken);

        // SaveChangesAsync é feito pelo TransactionBehavior (command).
        return Result<RunEdgeUpdateCycleResult>.Success(
            new RunEdgeUpdateCycleResult(nameof(EdgeUpdateStatus.RolledBack), health.FailureReason));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.skip: instalação edge ainda não importada.")]
    private partial void LogSemInstalacao();

    [LoggerMessage(Level = LogLevel.Information, Message = "edge.update.deferred: Instalacao={InstallationId} VersaoAlvo={TargetVersion} EventosPendentes={PendingEvents}")]
    private partial void LogAtualizacaoAdiada(Guid installationId, string targetVersion, int pendingEvents);

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.step_failed: Instalacao={InstallationId} Passo={Step} Motivo={FailureReason}")]
    private partial void LogPassoFalhou(Guid installationId, string step, string? failureReason);

    [LoggerMessage(Level = LogLevel.Information, Message = "edge.update.succeeded: Instalacao={InstallationId} Versao={Version}")]
    private partial void LogAtualizacaoConcluida(Guid installationId, string version);

    [LoggerMessage(Level = LogLevel.Warning, Message = "edge.update.rolled_back: Instalacao={InstallationId} VersaoAlvo={TargetVersion} VersaoAnterior={PreviousVersion} Motivo={FailureReason}")]
    private partial void LogRollback(Guid installationId, string targetVersion, string previousVersion, string? failureReason);
}
