namespace Awaken.Domain.Entities.Avatars;

/// US-234: metadado de um avatar interno de catalogo. RequiredItemKey aponta
/// para um pack ja cadastrado em Awaken.Domain.Entities.Inventory.ItemKeys -
/// quando null, o avatar esta liberado para todos (RN-002/RN-003).
public record AvatarCatalogEntry(string AvatarKey, string? RequiredItemKey = null);
