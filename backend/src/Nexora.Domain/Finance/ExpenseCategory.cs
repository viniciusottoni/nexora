using Nexora.Domain.Common;

namespace Nexora.Domain.Finance;

/// <summary>
/// Categoria de despesa/receita usada para classificar lançamentos financeiros e compor o CMV.
/// </summary>
public sealed class ExpenseCategory
{
    private ExpenseCategory() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ExpenseGroup Group { get; private set; }
    public bool IsCmv { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static ExpenseCategory Create(Guid tenantId, string name, ExpenseGroup group, bool isCmv = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da categoria de despesa é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new ExpenseCategory
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Group = group,
            IsCmv = isCmv,
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
