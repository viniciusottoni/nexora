namespace Nexora.Domain.Sync;

/// <summary>
/// Ponteiro de progresso de sincronização por instalação de edge e direção (push/pull).
/// Chave composta: <c>(installationId, direction)</c>. Sem enum próprio — <see cref="Direction"/>
/// segue como string livre (<c>PUSH</c>, <c>PULL</c>), igual ao schema Prisma original.
/// </summary>
public sealed class SyncCursor
{
    private SyncCursor() { }

    public Guid InstallationId { get; private set; }
    public string Direction { get; private set; } = string.Empty;
    public long LastSeq { get; private set; }
    public DateTimeOffset? LastSuccessAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static SyncCursor Create(Guid installationId, string direction)
    {
        return new SyncCursor
        {
            InstallationId = installationId,
            Direction = direction,
            LastSeq = 0,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Advance(long lastSeq)
    {
        LastSeq = lastSeq;
        LastSuccessAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
