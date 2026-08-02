using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Contagem de um insumo dentro de um inventário físico — compara a quantidade esperada
/// (derivada dos movimentos) com a quantidade efetivamente contada.
/// </summary>
public sealed class InventoryCountItem
{
    private InventoryCountItem() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CountId { get; private set; }
    public Guid IngredientId { get; private set; }
    public decimal ExpectedQty { get; private set; }
    public decimal CountedQty { get; private set; }
    public decimal DivergenceQty { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal? DivergenceCost { get; private set; }

    public static InventoryCountItem Create(
        Guid tenantId,
        Guid countId,
        Guid ingredientId,
        decimal expectedQty,
        decimal countedQty,
        decimal unitCost)
    {
        if (expectedQty < 0)
            throw new DomainException("A quantidade esperada do item de inventário não pode ser negativa.");

        if (countedQty < 0)
            throw new DomainException("A quantidade contada do item de inventário não pode ser negativa.");

        if (unitCost < 0)
            throw new DomainException("O custo unitário do item de inventário não pode ser negativo.");

        var divergenceQty = countedQty - expectedQty;

        // Arredondamento half-up na apuração do custo da divergência — ADR-017.
        var divergenceCost = Math.Round(divergenceQty * unitCost, 2, MidpointRounding.AwayFromZero);

        return new InventoryCountItem
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            CountId = countId,
            IngredientId = ingredientId,
            ExpectedQty = expectedQty,
            CountedQty = countedQty,
            DivergenceQty = divergenceQty,
            UnitCost = unitCost,
            DivergenceCost = divergenceCost
        };
    }
}
