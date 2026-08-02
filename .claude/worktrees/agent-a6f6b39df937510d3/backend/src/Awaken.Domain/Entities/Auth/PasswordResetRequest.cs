using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Auth;

public class PasswordResetRequest : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private PasswordResetRequest() { }

    public static PasswordResetRequest Create(
        Guid userId,
        string tokenHash,
        DateTime requestedAtUtc,
        DateTime expiresAtUtc) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            RequestedAtUtc = requestedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc < utcNow;

    public void MarkUsed(DateTime utcNow)
    {
        UsedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
