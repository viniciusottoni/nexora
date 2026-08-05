namespace Nexora.Contracts.Tenants;

/// <summary>Uma linha do diretório de estabelecimentos (US-151 §7, <c>GET /v1/platform/tenants</c>).</summary>
public sealed record TenantDirectoryEntryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string Plan,
    string? OwnerEmail,
    int StoresCount,
    int InstallationsCount,
    string Health,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Eco normalizado dos filtros efetivamente aplicados (US-151 §7/§10, "busca e filtros devem
/// permanecer refletidos na URL") — sempre as mesmas chaves, com arrays vazios/valores nulos para o
/// que não foi informado (nunca omitidas), para o front reconstruir a URL/chips sem heurística.
/// </summary>
public sealed record TenantDirectoryAppliedFiltersResponse(
    string? Query,
    IReadOnlyList<string> Status,
    IReadOnlyList<string> Plan,
    IReadOnlyList<string> Template,
    IReadOnlyList<string> Health,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    string Sort,
    int Limit);

public sealed record TenantDirectoryListResponse(
    IReadOnlyList<TenantDirectoryEntryResponse> Data,
    string? NextCursor,
    TenantDirectoryAppliedFiltersResponse AppliedFilters);
