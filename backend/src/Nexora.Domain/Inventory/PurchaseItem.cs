using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>Item de uma nota de compra — quantidade e custo de um insumo recebido de um fornecedor.</summary>
public sealed class PurchaseItem
{
    private PurchaseItem() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PurchaseId { get; private set; }
    public Guid IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UomCode { get; private set; } = string.Empty;
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public string? LotCode { get; private set; }

    public static PurchaseItem Create(
        Guid tenantId,
        Guid purchaseId,
        Guid ingredientId,
        decimal quantity,
        string uomCode,
        decimal unitCost,
        decimal totalCost,
        DateOnly? expiresAt = null,
        string? lotCode = null)
    {
        if (quantity <= 0)
            throw new DomainException("A quantidade do item de compra deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(uomCode))
            throw new DomainException("A unidade de medida do item de compra é obrigatória.");

        if (unitCost < 0)
            throw new DomainException("O custo unitário do item de compra não pode ser negativo.");

        if (totalCost < 0)
            throw new DomainException("O custo total do item de compra não pode ser negativo.");

        return new PurchaseItem
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            PurchaseId = purchaseId,
            IngredientId = ingredientId,
            Quantity = quantity,
            UomCode = uomCode,
            UnitCost = unitCost,
            TotalCost = totalCost,
            ExpiresAt = expiresAt,
            LotCode = lotCode
        };
    }
}
