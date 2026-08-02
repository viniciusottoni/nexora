using Nexora.Domain.Common;

namespace Nexora.Domain.Catalog;

/// <summary>Opção dentro de um <see cref="ModifierGroup"/> (ex.: "Bacon", "Sem cebola").</summary>
public sealed class Modifier
{
    private Modifier() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal PriceDelta { get; private set; }
    public Guid? IngredientId { get; private set; }
    public decimal? Quantity { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public short SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public ModifierGroup Group { get; private set; } = null!;

    public static Modifier Create(
        Guid tenantId,
        Guid groupId,
        string name,
        decimal priceDelta = 0m,
        Guid? ingredientId = null,
        decimal? quantity = null,
        short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do modificador é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Modifier
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            GroupId = groupId,
            Name = name,
            PriceDelta = priceDelta,
            IngredientId = ingredientId,
            Quantity = quantity,
            IsAvailable = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkUnavailable()
    {
        IsAvailable = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAvailable()
    {
        IsAvailable = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePrice(decimal priceDelta)
    {
        PriceDelta = priceDelta;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
