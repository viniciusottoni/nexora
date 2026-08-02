using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>
/// Corpo de <c>POST /v1/catalog/variants/:id/prices</c> (US-011 §7) — define o preço vigente de um
/// canal (padrão <c>DineIn</c> quando <see cref="Channel"/> é nulo), fechando automaticamente o
/// preço anterior do mesmo canal (<c>Price.Close</c> + novo <c>Price.Create</c>, histórico
/// preservado). Só um canal por chamada: a tabela de preço por canal completa (todos os canais
/// editáveis juntos, ajuste em massa, auditoria dedicada) é escopo da US-014.
/// </summary>
public sealed record SetVariantPriceRequest(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    string? Channel);
