namespace Awaken.Domain.Entities.Inventory;

/// US-187: metadado de um item de inventario. Nao tem preco (isso e
/// responsabilidade do ShopCatalog, que sera migrado para este catalogo em
/// uma US futura - ver US-188) nem efeito (RN-003).
public record ItemCatalogEntry(string ItemKey, ItemType Type, int MaxQuantity = 99);
