using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Queries.GetKdsHistory;

/// <summary>
/// Porta de <c>GET /v1/kds/history?shift=current&amp;stationId=...&amp;search=...</c> (US-046,
/// Histórico do turno no KDS) — itens já SERVIDOS de uma praça dentro do dia operacional CORRENTE
/// (ADR-018), do mais recente para o mais antigo. <paramref name="Search"/> filtra por substring
/// (sem diferenciar maiúsculas/minúsculas) tanto no código curto do pedido quanto no rótulo da
/// mesa — um único campo de busca cobre os dois (US-046 §10: "busca por código curto como caminho
/// principal", mas a mesa também precisa funcionar, cenário Gherkin "Busca por mesa"). <c>shift</c>
/// não chega até aqui: hoje só existe o turno corrente, sempre calculado a partir do relógio do
/// servidor no handler — o parâmetro só existe no controller por simetria com o contrato §7 da
/// história.
/// </summary>
public sealed record GetKdsHistoryQuery(Guid StationId, string? Search) : IQuery<GetKdsHistoryResponse>;
