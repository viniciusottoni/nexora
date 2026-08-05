using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Cashier;

/// <summary>
/// Contrato de US-053 (Taxa de serviço com retirada registrada) — <c>POST
/// /v1/sessions/{id}/service-fee/waive</c>. Diferente do <c>POST /v1/sessions/{id}/bill/waive-service-fee</c>
/// de US-027 (retirada efêmera, recalculada a cada consulta, sem persistir no agregado): este
/// endpoint é o registro AUTORITATIVO no nível da sessão (RN-010), usado pelo pagamento (US-052).
/// </summary>
/// <param name="Scope"><c>FULL</c> (toda a conta) ou <c>PARTIAL</c> (só a parte de uma pessoa, requer <see cref="Person"/> e sessão com divisão por pessoa ativa).</param>
public sealed record WaiveSessionServiceFeeRequest(string Reason, string Scope, int? Person = null);

public sealed record ServiceFeeWaivedSessionResponse(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal ServiceFee,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total);

public sealed record WaiveSessionServiceFeeResponse(ServiceFeeWaivedSessionResponse Session);
