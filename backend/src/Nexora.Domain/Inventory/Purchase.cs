using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>Nota de compra de insumos junto a um fornecedor, composta por <see cref="PurchaseItem"/>.</summary>
public sealed class Purchase
{
    private readonly List<PurchaseItem> _items = new();

    private Purchase() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? Document { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset PurchasedAt { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public IReadOnlyCollection<PurchaseItem> Items => _items.AsReadOnly();

    public static Purchase Create(
        Guid tenantId,
        Guid storeId,
        DateTimeOffset purchasedAt,
        DateOnly businessDay,
        Guid? supplierId = null,
        string? document = null,
        string? notes = null,
        Guid? createdBy = null)
    {
        return new Purchase
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            SupplierId = supplierId,
            Document = document,
            Total = 0,
            PurchasedAt = purchasedAt,
            BusinessDay = businessDay,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public PurchaseItem AddItem(
        Guid ingredientId,
        decimal quantity,
        string uomCode,
        decimal unitCost,
        decimal totalCost,
        DateOnly? expiresAt = null,
        string? lotCode = null)
    {
        var item = PurchaseItem.Create(TenantId, Id, ingredientId, quantity, uomCode, unitCost, totalCost, expiresAt, lotCode);
        _items.Add(item);
        Total += totalCost;
        return item;
    }
}
