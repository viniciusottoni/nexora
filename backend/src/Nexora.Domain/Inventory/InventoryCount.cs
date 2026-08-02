using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Inventário físico de uma loja em um dia operacional — conjunto de contagens
/// (<see cref="InventoryCountItem"/>) que, ao ser aplicado, gera movimentos de ajuste de estoque.
/// O status não é um enum nativo do Postgres no schema de origem (é <c>VARCHAR</c> livre,
/// como em <c>email_outbox.status</c>/<c>idempotency_key.status</c>), então é mantido como
/// <see cref="string"/> aqui — valores conhecidos: <c>OPEN</c>, <c>APPLIED</c>.
/// </summary>
public sealed class InventoryCount
{
    private readonly List<InventoryCountItem> _items = new();

    private InventoryCount() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public string Status { get; private set; } = "OPEN";
    public DateTimeOffset CountedAt { get; private set; }
    public Guid CountedBy { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }
    public decimal? TotalDivergenceCost { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<InventoryCountItem> Items => _items.AsReadOnly();

    public static InventoryCount Create(
        Guid tenantId,
        Guid storeId,
        DateOnly businessDay,
        DateTimeOffset countedAt,
        Guid countedBy,
        string? notes = null)
    {
        return new InventoryCount
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            BusinessDay = businessDay,
            Status = "OPEN",
            CountedAt = countedAt,
            CountedBy = countedBy,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public InventoryCountItem AddItem(Guid ingredientId, decimal expectedQty, decimal countedQty, decimal unitCost)
    {
        if (Status != "OPEN")
            throw new DomainException("Só é possível adicionar contagens a um inventário em aberto.");

        var item = InventoryCountItem.Create(TenantId, Id, ingredientId, expectedQty, countedQty, unitCost);
        _items.Add(item);
        TotalDivergenceCost = _items.Sum(i => i.DivergenceCost ?? 0);
        return item;
    }

    public void Apply(DateTimeOffset appliedAt)
    {
        if (Status != "OPEN")
            throw new DomainException("Só é possível aplicar um inventário em aberto.");

        Status = "APPLIED";
        AppliedAt = appliedAt;
    }
}
