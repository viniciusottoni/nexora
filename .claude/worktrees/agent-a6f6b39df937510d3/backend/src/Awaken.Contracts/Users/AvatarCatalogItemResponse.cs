namespace Awaken.Contracts.Users;

public record AvatarCatalogItemResponse(
    string AvatarKey,
    bool IsUnlocked,
    bool IsSelected,
    string? RequiredItemKey);
