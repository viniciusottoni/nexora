using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Código de pareamento de dispositivo, de uso único e com expiração curta.</summary>
public sealed class PairingCode
{
    private PairingCode() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public short Attempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static PairingCode Create(Guid tenantId, Guid storeId, string codeHash, Guid createdBy, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
            throw new DomainException("O código de pareamento é obrigatório.");

        return new PairingCode
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            CodeHash = codeHash,
            CreatedBy = createdBy,
            ExpiresAt = expiresAt,
            Attempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsConsumed => ConsumedAt is not null;

    public void RecordAttempt()
    {
        Attempts++;
    }

    public void Consume()
    {
        if (IsConsumed)
            throw new DomainException("Este código de pareamento já foi utilizado.");

        ConsumedAt = DateTimeOffset.UtcNow;
    }
}
