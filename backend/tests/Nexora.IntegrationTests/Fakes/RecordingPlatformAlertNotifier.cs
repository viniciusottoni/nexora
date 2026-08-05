using Nexora.Application.Abstractions.Notifications;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="IPlatformAlertNotifier"/> (US-140 §4, cenário "Instalação fora do
/// ar" — "a plataforma deve ser alertada automaticamente") — grava as chamadas em memória para o
/// teste afirmar que a notificação realmente disparou, sem depender de um canal de infraestrutura
/// real. Mesmo espírito de <c>RecordingSyncStatusBroadcaster</c>/<c>RecordingAlertsBroadcaster</c>.
/// </summary>
public sealed class RecordingPlatformAlertNotifier : IPlatformAlertNotifier
{
    private readonly List<InstallationDownCall> _calls = new();

    public IReadOnlyList<InstallationDownCall> Calls => _calls.AsReadOnly();

    public Task NotifyInstallationDownAsync(
        Guid tenantId,
        Guid installationId,
        string installationLabel,
        DateTimeOffset? lastSeenAt,
        CancellationToken cancellationToken)
    {
        _calls.Add(new InstallationDownCall(tenantId, installationId, installationLabel, lastSeenAt));
        return Task.CompletedTask;
    }

    private readonly List<CertificateRenewalFailedCall> _certificateRenewalFailedCalls = new();

    public IReadOnlyList<CertificateRenewalFailedCall> CertificateRenewalFailedCalls => _certificateRenewalFailedCalls.AsReadOnly();

    /// <summary>US-143 §4, cenário "Falha de renovação" — mesma porta (US-140) reaproveitada, agora exercitada por <c>TenantDomainIntegrationTests</c>.</summary>
    public Task NotifyDomainCertificateRenewalFailedAsync(
        Guid tenantId,
        Guid domainId,
        string domain,
        DateTimeOffset? certExpiresAt,
        CancellationToken cancellationToken)
    {
        _certificateRenewalFailedCalls.Add(new CertificateRenewalFailedCall(tenantId, domainId, domain, certExpiresAt));
        return Task.CompletedTask;
    }

    private readonly List<EdgeUpdateRolledBackCall> _edgeUpdateRolledBackCalls = new();

    public IReadOnlyList<EdgeUpdateRolledBackCall> EdgeUpdateRolledBackCalls => _edgeUpdateRolledBackCalls.AsReadOnly();

    /// <summary>US-146 §4, cenário "Rollback automático" — mesma porta (US-140) reaproveitada.</summary>
    public Task NotifyEdgeUpdateRolledBackAsync(
        Guid tenantId, Guid installationId, string targetVersion, string previousVersion, string? failureReason, CancellationToken cancellationToken)
    {
        _edgeUpdateRolledBackCalls.Add(new EdgeUpdateRolledBackCall(tenantId, installationId, targetVersion, previousVersion, failureReason));
        return Task.CompletedTask;
    }

    private readonly List<EdgeUpdateDeferredCall> _edgeUpdateDeferredCalls = new();

    public IReadOnlyList<EdgeUpdateDeferredCall> EdgeUpdateDeferredCalls => _edgeUpdateDeferredCalls.AsReadOnly();

    /// <summary>US-146 §4, cenário "Instalação com pendência de sincronização" — mesma porta (US-140) reaproveitada.</summary>
    public Task NotifyEdgeUpdateDeferredAsync(
        Guid tenantId, Guid installationId, string targetVersion, int pendingEvents, CancellationToken cancellationToken)
    {
        _edgeUpdateDeferredCalls.Add(new EdgeUpdateDeferredCall(tenantId, installationId, targetVersion, pendingEvents));
        return Task.CompletedTask;
    }

    public sealed record InstallationDownCall(Guid TenantId, Guid InstallationId, string InstallationLabel, DateTimeOffset? LastSeenAt);

    public sealed record CertificateRenewalFailedCall(Guid TenantId, Guid DomainId, string Domain, DateTimeOffset? CertExpiresAt);

    public sealed record EdgeUpdateRolledBackCall(Guid TenantId, Guid InstallationId, string TargetVersion, string PreviousVersion, string? FailureReason);

    public sealed record EdgeUpdateDeferredCall(Guid TenantId, Guid InstallationId, string TargetVersion, int PendingEvents);
}
