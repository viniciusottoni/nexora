using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>Linha de preço historizada de uma variante (US-011 §8) — <see cref="ValidTo"/> nulo é a linha vigente.</summary>
public sealed record PriceResponse(
    Guid Id,
    Guid VariantId,
    string Channel,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);
