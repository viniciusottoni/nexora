namespace Nexora.Contracts.Operation;

/// <summary>Porta de <c>createAreaRequestSchema</c> (<c>packages/contracts/src/operation-areas.ts</c>). <c>POST /v1/areas</c>.</summary>
public sealed record CreateAreaRequest(string Name, short Position);

/// <summary>Porta de <c>updateAreaRequestSchema</c>. <c>PATCH /v1/areas/{id}</c>.</summary>
public sealed record UpdateAreaRequest(string Name, short Position);

/// <summary>Porta de <c>areaSchema</c>.</summary>
public sealed record AreaResponse(Guid Id, string Name, short Position, bool Active, int TableCount);

/// <summary>Porta de <c>areaListResponseSchema</c>. <c>GET /v1/areas</c>.</summary>
public sealed record AreaListResponse(IReadOnlyList<AreaResponse> Items);
