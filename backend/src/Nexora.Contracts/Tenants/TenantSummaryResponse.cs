namespace Nexora.Contracts.Tenants;

/// <summary>Espelha <c>TenantSummaryDto</c> do NestJS original.</summary>
public sealed record TenantSummaryResponse(
    Guid Id,
    string Slug,
    string Name,
    string Plan,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record TenantListResponse(IReadOnlyList<TenantSummaryResponse> Data);

public sealed record SlugAvailabilityResponse(string Slug, bool Available);
