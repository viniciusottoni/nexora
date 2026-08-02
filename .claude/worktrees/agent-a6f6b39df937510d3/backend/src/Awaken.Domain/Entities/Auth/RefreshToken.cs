using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Auth;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
        };

    public void Revoke(DateTime utcNow)
    {
        IsRevoked = true;
        UpdatedAtUtc = utcNow;
    }
}
