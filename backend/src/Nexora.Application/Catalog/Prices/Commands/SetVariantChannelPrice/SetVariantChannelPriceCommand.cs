using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Prices.Commands.SetVariantChannelPrice;

/// <summary>
/// Define o preço vigente de uma variante em um ou mais canais na mesma chamada (US-014 §7),
/// fechando automaticamente o preço anterior de cada canal informado e criando uma nova linha —
/// nunca edita uma linha de <c>Price</c> existente (imutável por design, mesma regra da US-011).
/// Porta de <c>PUT /v1/catalog/variants/{id}/prices</c>.
/// </summary>
public sealed record SetVariantChannelPriceCommand(Guid VariantId, IReadOnlyList<ChannelPriceEntry> Prices)
    : ICommand<VariantPriceTableResponse>;
