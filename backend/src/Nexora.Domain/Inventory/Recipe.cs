using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Ficha técnica de um produto (ou de uma sub-receita usada por outras fichas técnicas),
/// composta por itens que referenciam insumos ou sub-receitas (<see cref="RecipeItem"/>).
/// </summary>
public sealed class Recipe
{
    private readonly List<RecipeItem> _items = new();

    private Recipe() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string? Name { get; private set; }
    public bool IsSubRecipe { get; private set; }
    public decimal YieldQty { get; private set; } = 1m;
    public string? YieldUom { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<RecipeItem> Items => _items.AsReadOnly();

    public static Recipe Create(
        Guid tenantId,
        decimal yieldQty = 1m,
        Guid? variantId = null,
        string? name = null,
        bool isSubRecipe = false,
        string? yieldUom = null,
        string? notes = null)
    {
        if (yieldQty <= 0)
            throw new DomainException("O rendimento da ficha técnica deve ser maior que zero.");

        var now = DateTimeOffset.UtcNow;

        return new Recipe
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            VariantId = variantId,
            Name = name,
            IsSubRecipe = isSubRecipe,
            YieldQty = yieldQty,
            YieldUom = yieldUom,
            Notes = notes,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public RecipeItem AddItem(
        decimal quantity,
        string uomCode,
        Guid? ingredientId = null,
        Guid? subRecipeId = null,
        decimal wastePercent = 0,
        short sortOrder = 0)
    {
        var item = RecipeItem.Create(TenantId, Id, quantity, uomCode, ingredientId, subRecipeId, wastePercent, sortOrder);
        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        return item;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
