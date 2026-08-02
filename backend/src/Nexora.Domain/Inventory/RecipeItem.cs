using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Componente de uma ficha técnica — referencia exatamente um insumo ou uma sub-receita, nunca os dois.
/// </summary>
public sealed class RecipeItem
{
    private RecipeItem() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RecipeId { get; private set; }
    public Guid? IngredientId { get; private set; }
    public Guid? SubRecipeId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UomCode { get; private set; } = string.Empty;
    public decimal WastePercent { get; private set; }
    public short SortOrder { get; private set; }

    public static RecipeItem Create(
        Guid tenantId,
        Guid recipeId,
        decimal quantity,
        string uomCode,
        Guid? ingredientId = null,
        Guid? subRecipeId = null,
        decimal wastePercent = 0,
        short sortOrder = 0)
    {
        if (quantity <= 0)
            throw new DomainException("A quantidade do item da ficha técnica deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(uomCode))
            throw new DomainException("A unidade de medida do item da ficha técnica é obrigatória.");

        if (wastePercent < 0)
            throw new DomainException("O percentual de perda do item da ficha técnica não pode ser negativo.");

        if (ingredientId is null && subRecipeId is null)
            throw new DomainException("O item da ficha técnica precisa referenciar um insumo ou uma sub-receita.");

        if (ingredientId is not null && subRecipeId is not null)
            throw new DomainException("O item da ficha técnica não pode referenciar um insumo e uma sub-receita ao mesmo tempo.");

        return new RecipeItem
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            RecipeId = recipeId,
            IngredientId = ingredientId,
            SubRecipeId = subRecipeId,
            Quantity = quantity,
            UomCode = uomCode,
            WastePercent = wastePercent,
            SortOrder = sortOrder
        };
    }
}
