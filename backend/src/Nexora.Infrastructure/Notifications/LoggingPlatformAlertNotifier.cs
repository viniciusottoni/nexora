using Nexora.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// <see cref="IPlatformAlertNotifier"/> de fallback (log estruturado) — mesmo idioma de
/// <see cref="LoggingEmailDispatcher"/>: o mecanismo (worker de saúde de instalação →
/// <c>EvaluateInstallationHealthCommand</c> → esta notificação) roda de ponta a ponta; só o canal
/// de entrega real (e-mail/Slack/PagerDuty da equipe de plataforma) fica para uma infraestrutura
/// futura. Registrado por padrão em <c>Nexora.Api.Cloud/Program.cs</c> — não há hoje configuração
/// condicional (ex.: webhook configurado) que troque a implementação, ao contrário de
/// <see cref="LoggingEmailDispatcher"/>/<see cref="SmtpEmailDispatcher"/>.
/// </summary>
public sealed partial class LoggingPlatformAlertNotifier : IPlatformAlertNotifier
{
    private readonly ILogger<LoggingPlatformAlertNotifier> _logger;

    public LoggingPlatformAlertNotifier(ILogger<LoggingPlatformAlertNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyInstallationDownAsync(
        Guid tenantId,
        Guid installationId,
        string installationLabel,
        DateTimeOffset? lastSeenAt,
        CancellationToken cancellationToken)
    {
        LogInstalacaoForaDoAr(tenantId, installationId, installationLabel, lastSeenAt);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "platform.alert.installation_down: Tenant={TenantId} Instalacao={InstallationId} Rotulo={Label} UltimoContato={LastSeenAt}")]
    private partial void LogInstalacaoForaDoAr(Guid tenantId, Guid installationId, string label, DateTimeOffset? lastSeenAt);

    /// <summary>US-143 §4 "Falha de renovação" — mesmo canal de fallback (log estruturado) até haver um canal real.</summary>
    public Task NotifyDomainCertificateRenewalFailedAsync(
        Guid tenantId, Guid domainId, string domain, DateTimeOffset? certExpiresAt, CancellationToken cancellationToken)
    {
        LogRenovacaoDeCertificadoFalhou(tenantId, domainId, domain, certExpiresAt);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "platform.alert.domain_certificate_renewal_failed: Tenant={TenantId} Dominio={DomainId} Host={Domain} Vencimento={CertExpiresAt}")]
    private partial void LogRenovacaoDeCertificadoFalhou(Guid tenantId, Guid domainId, string domain, DateTimeOffset? certExpiresAt);

    /// <summary>US-146 §4 "Rollback automático" — mesmo canal de fallback (log estruturado) até haver um canal real.</summary>
    public Task NotifyEdgeUpdateRolledBackAsync(
        Guid tenantId, Guid installationId, string targetVersion, string previousVersion, string? failureReason, CancellationToken cancellationToken)
    {
        LogAtualizacaoRevertida(tenantId, installationId, targetVersion, previousVersion, failureReason);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "platform.alert.edge_update_rolled_back: Tenant={TenantId} Instalacao={InstallationId} VersaoAlvo={TargetVersion} VersaoAnterior={PreviousVersion} Motivo={FailureReason}")]
    private partial void LogAtualizacaoRevertida(Guid tenantId, Guid installationId, string targetVersion, string previousVersion, string? failureReason);

    /// <summary>US-146 §4 "Instalação com pendência de sincronização" — mesmo canal de fallback (log estruturado) até haver um canal real.</summary>
    public Task NotifyEdgeUpdateDeferredAsync(
        Guid tenantId, Guid installationId, string targetVersion, int pendingEvents, CancellationToken cancellationToken)
    {
        LogAtualizacaoAdiada(tenantId, installationId, targetVersion, pendingEvents);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "platform.alert.edge_update_deferred: Tenant={TenantId} Instalacao={InstallationId} VersaoAlvo={TargetVersion} EventosPendentes={PendingEvents}")]
    private partial void LogAtualizacaoAdiada(Guid tenantId, Guid installationId, string targetVersion, int pendingEvents);
}
