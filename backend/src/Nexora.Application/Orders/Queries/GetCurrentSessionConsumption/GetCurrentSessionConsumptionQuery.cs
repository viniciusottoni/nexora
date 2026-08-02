using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Queries.GetCurrentSessionConsumption;

/// <summary>
/// Porta de <c>GET /v1/public/sessions/current</c> (US-024 §7) — SEM parâmetro de sessão: a sessão
/// é sempre resolvida a partir da claim <c>ses</c> do token de sessão de mesa corrente
/// (<see cref="Application.Abstractions.Security.ICurrentTenantContext.SessionId"/>), nunca de um
/// id informado pelo chamador. É essa ausência de parâmetro — e não uma checagem explícita de
/// "sessão X pertence à mesa Y" — que garante ADR-021/RN-015 (token de uma mesa nunca acessa o
/// consumo de outra): não existe nenhum jeito de o cliente pedir a sessão de outra mesa, porque a
/// rota não aceita um id para substituir.
/// </summary>
public sealed record GetCurrentSessionConsumptionQuery : IQuery<SessionConsumptionResponse>;
