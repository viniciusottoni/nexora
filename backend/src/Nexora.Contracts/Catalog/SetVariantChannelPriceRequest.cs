using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>Um preço a definir para um canal específico — item de <see cref="SetVariantChannelPriceRequest"/>.</summary>
public sealed record ChannelPriceEntry(
    string Channel,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount);

/// <summary>
/// Corpo de <c>PUT /v1/catalog/variants/{id}/prices</c> (US-014 §7) — define o preço vigente de
/// um ou mais canais na mesma chamada (diferente de <c>POST .../prices</c> da US-011, que só
/// define um canal por vez). Cada canal enviado fecha automaticamente o preço vigente daquele
/// canal e cria uma nova linha — nunca edita uma linha de <c>Price</c> existente (imutável por
/// design). Canal repetido na mesma lista é recusado (<c>PRICE_TABLE_CHANNEL_DUPLICATED</c>).
/// </summary>
public sealed record SetVariantChannelPriceRequest(IReadOnlyList<ChannelPriceEntry> Prices);
