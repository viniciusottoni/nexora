using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Convite de posse/administração enviado a um e-mail, consumível uma única vez.</summary>
public sealed class OwnerInvite
{
    private OwnerInvite() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public AppUser User { get; private set; } = null!;

    public static OwnerInvite Create(Guid tenantId, Guid userId, string email, string secretHash, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do convite é obrigatório.");

        if (string.IsNullOrWhiteSpace(secretHash))
            throw new DomainException("O segredo do convite é obrigatório.");

        return new OwnerInvite
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            UserId = userId,
            Email = email,
            SecretHash = secretHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsConsumed => ConsumedAt is not null;

    public void Consume()
    {
        if (IsConsumed)
            throw new DomainException("Este convite já foi utilizado.");

        ConsumedAt = DateTimeOffset.UtcNow;
    }
}
