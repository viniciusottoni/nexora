namespace Nexora.Contracts.Roles;

public sealed record RoleResponse(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyList<string> Permissions,
    bool System,
    int UserCount);

public sealed record PermissionCatalogItemResponse(string Code, string Resource, string Description, bool Sensitive);

public sealed record RoleListResponse(
    IReadOnlyList<RoleResponse> Items,
    IReadOnlyList<PermissionCatalogItemResponse> PermissionCatalog);
