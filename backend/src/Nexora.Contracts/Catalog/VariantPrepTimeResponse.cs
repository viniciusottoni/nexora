namespace Nexora.Contracts.Catalog;

/// <summary>US-016 — retorno de <c>PATCH /v1/catalog/variants/{id}/prep-time</c>.</summary>
public sealed record VariantPrepTimeResponse(Guid VariantId, short PrepMinutes, short? WarnMinutes, short? CriticalMinutes);
