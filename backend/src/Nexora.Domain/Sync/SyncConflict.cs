using Nexora.Domain.Common;

namespace Nexora.Domain.Sync;

/// <summary>
/// Conflito detectado durante a sincronização entre edge e nuvem — registrado para revisão
/// manual ou auditoria (ADR-006).
/// </summary>
public sealed class SyncConflict
{
    private SyncConflict() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? InstallationId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string Resolution { get; private set; } = string.Empty;

    // TODO: tipar quando o formato do payload de conflito for definido
    public string? Payload { get; private set; }

    public DateTimeOffset DetectedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    public static SyncConflict Create(Guid tenantId, Guid eventId, string eventType, string reason, string resolution, Guid? installationId = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do conflito de sincronização é obrigatório.");

        if (string.IsNullOrWhiteSpace(resolution))
            throw new DomainException("A resolução do conflito de sincronização é obrigatória.");

        return new SyncConflict
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            InstallationId = installationId,
            EventId = eventId,
            EventType = eventType,
            Reason = reason,
            Resolution = resolution,
            DetectedAt = DateTimeOffset.UtcNow
        };
    }

    public void Review(Guid reviewedBy)
    {
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTimeOffset.UtcNow;
    }
}
