namespace Nexora.Application.Abstractions.Notifications;

/// <summary>
/// Notifica "a plataforma" (equipe da Replay) quando uma instalação transiciona para DOWN
/// (US-140 §4, cenário "Instalação fora do ar" — "a plataforma deve ser alertada automaticamente").
/// Porta em Application; implementação real (Infrastructure) hoje é só log estruturado — mesmo
/// idioma de <c>IEmailDispatcher</c>/<c>LoggingEmailDispatcher</c> (infraestrutura real por trás de
/// uma interface, com um padrão de log como default seguro para dev/CI). Um canal de notificação
/// efetivo (e-mail/Slack/PagerDuty da equipe de plataforma) é um próximo passo de infraestrutura,
/// não desta história.
/// </summary>
public interface IPlatformAlertNotifier
{
    Task NotifyInstallationDownAsync(
        Guid tenantId,
        Guid installationId,
        string installationLabel,
        DateTimeOffset? lastSeenAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// US-143 §4, cenário "Falha de renovação" — "a plataforma deve ser alertada" quando a
    /// renovação automática do certificado TLS de um domínio próprio falha
    /// (<c>RenewTenantDomainCertificateCommandHandler</c>, disparado pelo
    /// <c>TenantDomainCertificateRenewalWorker</c> quando <c>TenantDomain.IsCertificateExpiringSoon</c>
    /// vira verdadeiro). Reaproveita esta porta (US-140) em vez de criar uma nova — mesmo "alertar
    /// a plataforma", canal ainda por vir.
    /// </summary>
    Task NotifyDomainCertificateRenewalFailedAsync(
        Guid tenantId,
        Guid domainId,
        string domain,
        DateTimeOffset? certExpiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// US-146 §4, cenário "Rollback automático" — "a plataforma deve ser alertada" quando o health
    /// check pós-migration falha e <c>RunEdgeUpdateCycleCommandHandler</c> reverte automaticamente
    /// para a versão anterior. Reaproveita esta porta (US-140) em vez de criar uma nova.
    /// </summary>
    Task NotifyEdgeUpdateRolledBackAsync(
        Guid tenantId,
        Guid installationId,
        string targetVersion,
        string previousVersion,
        string? failureReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// US-146 §4, cenário "Instalação com pendência de sincronização" — "a plataforma deve ser
    /// informada" quando a atualização é adiada por haver eventos pendentes acima do limiar
    /// (<see cref="Nexora.Application.Installations.Support.EdgeUpdateSyncPendingPolicy"/>).
    /// </summary>
    Task NotifyEdgeUpdateDeferredAsync(
        Guid tenantId,
        Guid installationId,
        string targetVersion,
        int pendingEvents,
        CancellationToken cancellationToken);
}
