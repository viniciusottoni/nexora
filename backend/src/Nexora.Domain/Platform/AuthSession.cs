using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Sessão de autenticação (refresh token) de um usuário, opcionalmente ligada a um dispositivo.</summary>
public sealed class AuthSession
{
    private AuthSession() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? DeviceId { get; private set; }
    public string? RefreshHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset LastActiveAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public AppUser User { get; private set; } = null!;
    public Device? Device { get; private set; }

    public static AuthSession Create(Guid tenantId, Guid userId, Guid? deviceId, string? refreshHash, DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;

        return new AuthSession
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            UserId = userId,
            DeviceId = deviceId,
            RefreshHash = refreshHash,
            ExpiresAt = expiresAt,
            LastActiveAt = now,
            CreatedAt = now
        };
    }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void RecordActivity()
    {
        LastActiveAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        if (IsRevoked)
            throw new DomainException("Esta sessão já foi revogada.");

        RevokedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Rotaciona o refresh token da sessão (novo hash e nova expiração) — porta de
    /// <c>rotateRefresh</c> (apps/api-cloud/src/modules/auth/prisma-password-auth.repository.ts),
    /// chamado a cada uso bem-sucedido de POST /v1/auth/refresh.
    /// </summary>
    public void Rotate(string refreshHash, DateTimeOffset expiresAt)
    {
        if (IsRevoked)
            throw new DomainException("Esta sessão já foi revogada.");

        if (string.IsNullOrWhiteSpace(refreshHash))
            throw new DomainException("O hash do novo refresh token é obrigatório.");

        RefreshHash = refreshHash;
        ExpiresAt = expiresAt;
        LastActiveAt = DateTimeOffset.UtcNow;
    }
}
