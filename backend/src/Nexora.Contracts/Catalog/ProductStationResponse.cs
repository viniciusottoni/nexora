namespace Nexora.Contracts.Catalog;

/// <summary>
/// US-016 — retorno de <c>PATCH /v1/catalog/products/{id}/station</c>. <see cref="StationCode"/>
/// e <see cref="StationName"/> vêm nulos quando <see cref="StationId"/> é nulo (produto sem praça).
/// </summary>
public sealed record ProductStationResponse(Guid ProductId, Guid? StationId, string? StationCode, string? StationName);
