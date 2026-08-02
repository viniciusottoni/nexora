namespace Nexora.Domain.Sync;

/// <summary>
/// Fila de saída de eventos de domínio a sincronizar entre o edge e a nuvem (ADR-006).
/// Chave composta: um evento pode ser reenfileirado, mas <c>(eventId, occurredAt)</c> é único.
/// Sem enum próprio — <see cref="Status"/> segue como string livre, igual ao schema Prisma
/// original (<c>PENDING</c>, <c>SYNCED</c>, <c>FAILED</c>...).
/// </summary>
public sealed class Outbox
{
    private Outbox() { }

    public Guid EventId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid TenantId { get; private set; }
    public long DeviceSeq { get; private set; }
    public string Status { get; private set; } = "PENDING";
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset? SyncedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Outbox Create(Guid eventId, DateTimeOffset occurredAt, Guid tenantId, long deviceSeq)
    {
        return new Outbox
        {
            EventId = eventId,
            OccurredAt = occurredAt,
            TenantId = tenantId,
            DeviceSeq = deviceSeq,
            Status = "PENDING",
            Attempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RegisterFailure(string error, DateTimeOffset nextRetryAt)
    {
        Status = "FAILED";
        Attempts++;
        LastError = error;
        NextRetryAt = nextRetryAt;
    }

    public void MarkSynced()
    {
        Status = "SYNCED";
        SyncedAt = DateTimeOffset.UtcNow;
    }
}
