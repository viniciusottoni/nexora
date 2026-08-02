namespace Nexora.Contracts.Catalog;

/// <summary>Uma fração escolhida pelo cliente/garçom — sabor (variante) e peso (US-013 §7).</summary>
public sealed record FractionSelectionRequest(Guid VariantId, decimal Weight);

/// <summary>
/// Corpo de <c>POST /v1/catalog/fraction-pricing/preview</c> (US-013) — calcula o preço final e a
/// descrição composta de um item meio a meio ANTES de qualquer confirmação, sem persistir nada
/// (não existe ainda módulo de Pedidos nesta solution — ver decisão de escopo no relatório da
/// tarefa que introduziu este endpoint). <see cref="Channel"/> ausente cai no canal padrão
/// (<c>DineIn</c>), mesmo espírito de <c>SetVariantPriceRequest</c> (US-011).
/// </summary>
public sealed record PreviewFractionPricingRequest(IReadOnlyList<FractionSelectionRequest> Fractions, string? Channel);
