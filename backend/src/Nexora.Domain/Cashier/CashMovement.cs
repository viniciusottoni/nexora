using Nexora.Domain.Common;

namespace Nexora.Domain.Cashier;

/// <summary>
/// Lançamento manual de caixa fora do fluxo de pagamento — sangria (retirada) ou suprimento (reforço).
/// </summary>
public sealed class CashMovement
{
    private CashMovement() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CashSessionId { get; private set; }
    public CashMovementType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static CashMovement Create(
        Guid tenantId,
        Guid cashSessionId,
        CashMovementType type,
        decimal amount,
        string reason,
        Guid createdBy,
        DateTimeOffset occurredAt,
        Guid? authorizedBy = null)
    {
        if (amount <= 0)
            throw new DomainException("O valor do movimento de caixa deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do movimento de caixa é obrigatório.");

        return new CashMovement
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            CashSessionId = cashSessionId,
            Type = type,
            Amount = amount,
            Reason = reason,
            CreatedBy = createdBy,
            AuthorizedBy = authorizedBy,
            OccurredAt = occurredAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
