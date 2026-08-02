using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Admin;

public class AdminUser : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public string? MfaSecretEncrypted { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string Status { get; private set; } = "active"; // "active" | "inactive"
    public DateTime? LastLoginAtUtc { get; private set; }

    private AdminUser() { }

    public static AdminUser Create(string email, string passwordHash, DateTime utcNow) =>
        new() { Email = email.ToLowerInvariant(), PasswordHash = passwordHash, CreatedAtUtc = utcNow };

    public bool IsLocked(DateTime utcNow) => LockedUntilUtc.HasValue && LockedUntilUtc > utcNow;

    public void RecordFailedLogin(DateTime utcNow)
    {
        FailedLoginAttempts++;
        UpdatedAtUtc = utcNow;
        if (FailedLoginAttempts >= 5)
            Lock(TimeSpan.FromMinutes(15), utcNow);
    }

    public void Lock(TimeSpan duration, DateTime utcNow)
    {
        LockedUntilUtc = utcNow.Add(duration);
        UpdatedAtUtc = utcNow;
    }

    public void Unlock(DateTime utcNow)
    {
        LockedUntilUtc = null;
        FailedLoginAttempts = 0;
        UpdatedAtUtc = utcNow;
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        LastLoginAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void SetMfaSecret(string encryptedSecret, DateTime utcNow)
    {
        MfaSecretEncrypted = encryptedSecret;
        UpdatedAtUtc = utcNow;
    }

    public void EnableMfa(DateTime utcNow)
    {
        MfaEnabled = true;
        UpdatedAtUtc = utcNow;
    }

    public void ResetMfa(DateTime utcNow)
    {
        MfaSecretEncrypted = null;
        MfaEnabled = false;
        UpdatedAtUtc = utcNow;
    }
}
