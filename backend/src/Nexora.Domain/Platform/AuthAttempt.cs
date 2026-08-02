using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Contador de tentativas de autenticação malsucedidas por dispositivo (rate limit / bloqueio).</summary>
public sealed class AuthAttempt
{
    private AuthAttempt() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DeviceId { get; private set; }
    public short FailedAttempts { get; private set; }
    public DateTimeOffset? BlockedUntil { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Device Device { get; private set; } = null!;

    public static AuthAttempt Create(Guid tenantId, Guid deviceId)
    {
        return new AuthAttempt
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            DeviceId = deviceId,
            FailedAttempts = 0,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RecordFailure()
    {
        FailedAttempts++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Block(DateTimeOffset until)
    {
        BlockedUntil = until;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsBlocked(DateTimeOffset now) => BlockedUntil is not null && now < BlockedUntil;

    public void Reset()
    {
        FailedAttempts = 0;
        BlockedUntil = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
