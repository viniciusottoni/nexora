using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Inventory;

/// US-187: estrutura de slot de inventario (ex.: slot de equipamento/cosmetico
/// equipado), sem nenhuma logica ou efeito associado ainda. Existe apenas para
/// suportar a evolucao futura do framework de itens (ADR-023); itens
/// concretos e regras de equipar/desequipar ficam fora desta US.
public class InventorySlot : BaseEntity
{
    public Guid UserId { get; private set; }
    public string SlotKey { get; private set; } = string.Empty;
    public string? ItemKey { get; private set; }

    private InventorySlot() { }

    public static InventorySlot Create(Guid userId, string slotKey, string? itemKey = null)
    {
        return new InventorySlot
        {
            UserId = userId,
            SlotKey = slotKey,
            ItemKey = itemKey,
        };
    }

    public void SetItem(string? itemKey, DateTime utcNow)
    {
        ItemKey = itemKey;
        UpdatedAtUtc = utcNow;
    }
}
