using Nexora.Domain.Common;

namespace Nexora.Domain.Finance;

/// <summary>
/// Conta financeira (caixa, banco, carteira digital) usada para registrar receitas e despesas.
/// </summary>
public sealed class FinancialAccount
{
    private FinancialAccount() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;

    // TODO: tipar quando o formato de bank_info for definido
    public string? BankInfo { get; private set; }

    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static FinancialAccount Create(Guid tenantId, string name, string type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da conta financeira é obrigatório.");

        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("O tipo da conta financeira é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new FinancialAccount
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Type = type,
            Balance = 0m,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
