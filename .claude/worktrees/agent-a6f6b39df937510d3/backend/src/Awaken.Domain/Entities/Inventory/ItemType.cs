namespace Awaken.Domain.Entities.Inventory;

/// US-187: tipo informativo do item, sem efeito implementado nesta fase
/// (RN-003). Serve apenas de metadado para o catalogo de chaves
/// (<see cref="ItemCatalog"/>) e para a apresentacao no app (ADR-021).
public enum ItemType
{
    Consumable = 0,
    Cosmetic = 2,
}
