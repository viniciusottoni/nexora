using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Movimento de estoque — o único ponto de escrita de variação de quantidade de um insumo
/// (ADR-008: o saldo é sempre derivado da soma de movimentos, nunca sincronizado diretamente).
/// </summary>
public sealed class StockMovement
{
    private StockMovement() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid IngredientId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public string UomCode { get; private set; } = string.Empty;
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public WasteReason? WasteReason { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? AuthorizedBy { get; private set; }

    public static StockMovement Create(
        Guid tenantId,
        Guid storeId,
        Guid ingredientId,
        DateOnly businessDay,
        StockMovementType type,
        decimal quantity,
        string uomCode,
        DateTimeOffset occurredAt,
        decimal? unitCost = null,
        decimal? totalCost = null,
        string? referenceType = null,
        Guid? referenceId = null,
        WasteReason? wasteReason = null,
        string? reason = null,
        Guid? createdBy = null,
        Guid? authorizedBy = null)
    {
        if (quantity == 0)
            throw new DomainException("A quantidade do movimento de estoque não pode ser zero.");

        if (string.IsNullOrWhiteSpace(uomCode))
            throw new DomainException("A unidade de medida do movimento de estoque é obrigatória.");

        if (type == StockMovementType.Waste && wasteReason is null)
            throw new DomainException("Um movimento de perda precisa informar o motivo.");

        return new StockMovement
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            IngredientId = ingredientId,
            BusinessDay = businessDay,
            Type = type,
            Quantity = quantity,
            UomCode = uomCode,
            UnitCost = unitCost,
            TotalCost = totalCost,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            WasteReason = wasteReason,
            Reason = reason,
            OccurredAt = occurredAt,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            AuthorizedBy = authorizedBy
        };
    }
}
