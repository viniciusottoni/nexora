namespace Nexora.Contracts.Catalog;

/// <summary>Uma fração já resolvida na resposta do preview — peso e preço vigente da variante no canal consultado.</summary>
public sealed record FractionPricingLineResponse(Guid VariantId, decimal Weight, decimal UnitPrice);

/// <summary>
/// Resposta de <c>POST /v1/catalog/fraction-pricing/preview</c> (US-013 §7/§10) — preço final já
/// calculado pela regra vigente do tenant, a regra efetivamente aplicada (para transparência no
/// comprovante/KDS) e a descrição composta (ex.: <c>"G · Mussarela / Calabresa"</c>, cenário
/// "Exibição no KDS" — ver nota de <c>PreviewFractionPricingQueryHandler</c> sobre por que o
/// prefixo não é literalmente <c>"Pizza"</c> como no exemplo do documento).
/// </summary>
public sealed record PreviewFractionPricingResponse(
    decimal UnitPrice,
    string PriceRule,
    string Description,
    IReadOnlyList<FractionPricingLineResponse> Fractions);
