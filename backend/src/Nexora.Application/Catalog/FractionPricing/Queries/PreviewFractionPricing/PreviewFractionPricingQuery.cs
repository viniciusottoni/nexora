using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.FractionPricing.Queries.PreviewFractionPricing;

/// <summary>
/// Preview de precificação de um item meio a meio (US-013) — calcula o preço final e a descrição
/// composta ANTES de qualquer confirmação, sem persistir nada. Porta de
/// <c>POST /v1/catalog/fraction-pricing/preview</c>. É <see cref="IQuery{TResponse}"/> (não
/// <see cref="ICommand{TResponse}"/>) porque nunca escreve — o <c>TransactionBehavior</c> do
/// pipeline MediatR não chama <c>SaveChangesAsync</c> para ela, mesmo o transporte HTTP sendo
/// <c>POST</c> (verbo escolhido só porque o corpo — lista de frações — não cabe numa querystring
/// de <c>GET</c>, não porque a operação grave algo).
/// </summary>
public sealed record PreviewFractionPricingQuery(
    IReadOnlyList<FractionSelectionRequest> Fractions,
    string? Channel) : IQuery<PreviewFractionPricingResponse>;
