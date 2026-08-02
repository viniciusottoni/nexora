using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Prices.Commands.SetVariantPrice;

/// <summary>
/// Define o preço vigente de uma variante em um canal (padrão <c>DineIn</c>), fechando
/// automaticamente o preço anterior do mesmo canal — nunca edita uma linha de <c>price</c>
/// existente (US-011 §"já pronto": <c>Price</c> é imutável por design). Porta de
/// <c>POST /v1/catalog/variants/{id}/prices</c>.
/// </summary>
public sealed record SetVariantPriceCommand(Guid VariantId, decimal Amount, string? Channel) : ICommand<PriceResponse>;
