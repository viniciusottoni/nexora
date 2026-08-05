using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Cashier;

/// <summary>
/// Contratos de US-054 (Desconto com autorização) — <c>POST /v1/sessions/{id}/discount</c>.
/// Exatamente um de <see cref="Percent"/>/<see cref="Amount"/> deve ser informado; o outro é
/// calculado e devolvido na resposta (US-054 §4, cenário "Desconto em valor absoluto": "o percentual
/// equivalente deve ser calculado e registrado").
/// </summary>
/// <param name="Scope"><c>SESSION</c> (sobre o total da conta) ou <c>ITEM</c> (sobre um item específico, requer <see cref="OrderItemId"/>).</param>
public sealed record ApplyDiscountRequest(
    decimal? Percent,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal? Amount,
    string Reason,
    string Scope,
    Guid? OrderItemId = null);

public sealed record DiscountedSessionResponse(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Discount,
    decimal DiscountPercent,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total);

public sealed record DiscountAuthorizerResponse(Guid Id, string Name);

public sealed record ApplyDiscountResponse(DiscountedSessionResponse Session, DiscountAuthorizerResponse? AuthorizedBy);
