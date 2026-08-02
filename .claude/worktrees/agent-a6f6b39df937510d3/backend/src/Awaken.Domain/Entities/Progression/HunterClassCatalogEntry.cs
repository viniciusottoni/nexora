namespace Awaken.Domain.Entities.Progression;

/// US-230: metadado de uma classe de Hunter. RequiredItemKey aponta para o
/// Pack correspondente (Awaken.Domain.Entities.Inventory.ItemKeys) — quando
/// null, a classe está liberada para todos (caso do beginner_hunter default).
public record HunterClassCatalogEntry(string ClassKey, string? RequiredItemKey = null);
