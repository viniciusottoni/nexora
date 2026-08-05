using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Queries.ListCashMovements;

/// <summary>Porta de <c>GET /v1/cash-sessions/current/movements</c> (US-056 §7/§10) — histórico do turno do operador corrente na loja.</summary>
public sealed record ListCashMovementsQuery : IQuery<ListCashMovementsResponse>;
