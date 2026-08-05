using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Concessão de acesso de suporte da Replay a um tenant (US-145) — única exceção autorizada ao
/// isolamento da RN-015, por isso precisa ser a mais controlada do produto: motivo obrigatório,
/// token de escopo especial com expiração curta, revogável pelo cliente a qualquer momento e
/// sempre visível na trilha do tenant. Complementa o registro em <c>audit_log</c>/EVT-074 já
/// emitido por <c>RecordSupportAccessCommand</c> (E-09/US-090) — aqui vive o CICLO DE VIDA do
/// acesso (expiração, revogação), que aquele comando não modelava.
/// </summary>
public sealed class SupportAccess
{
    private SupportAccess() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? GrantedTo { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static SupportAccess Grant(
        Guid tenantId, Guid? grantedTo, string reason, int durationMinutes, string tokenHash, DateTimeOffset grantedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do acesso de suporte é obrigatório.");

        if (durationMinutes <= 0)
            throw new DomainException("A duração do acesso de suporte deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O token do acesso de suporte é obrigatório.");

        return new SupportAccess
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            GrantedTo = grantedTo,
            Reason = reason,
            DurationMinutes = durationMinutes,
            TokenHash = tokenHash,
            GrantedAt = grantedAt,
            ExpiresAt = grantedAt.AddMinutes(durationMinutes),
            CreatedAt = grantedAt,
            UpdatedAt = grantedAt
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>Acesso utilizável agora — nem expirado, nem revogado (cenário "Nenhum acesso sem registro").</summary>
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public void Revoke(Guid? revokedBy, DateTimeOffset revokedAt)
    {
        if (IsRevoked)
            return;

        RevokedAt = revokedAt;
        RevokedBy = revokedBy;
        UpdatedAt = revokedAt;
    }

    public void RecordUsage(DateTimeOffset usedAt)
    {
        LastUsedAt = usedAt;
        UpdatedAt = usedAt;
    }
}
