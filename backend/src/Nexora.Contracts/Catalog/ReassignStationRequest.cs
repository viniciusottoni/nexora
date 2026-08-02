namespace Nexora.Contracts.Catalog;

/// <summary>
/// US-016 — corpo de <c>PATCH /v1/catalog/products/{id}/station</c>. <see cref="StationId"/> nulo
/// remove o vínculo do produto com qualquer praça de produção.
/// </summary>
public sealed record ReassignStationRequest(Guid? StationId);
