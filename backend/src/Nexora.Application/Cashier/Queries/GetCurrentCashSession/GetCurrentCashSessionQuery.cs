using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Queries.GetCurrentCashSession;

/// <summary>Porta de <c>GET /v1/cash-sessions/current</c> (US-055 §7) — sessão aberta/em conferência do operador corrente na loja, com a composição do valor esperado.</summary>
public sealed record GetCurrentCashSessionQuery : IQuery<GetCurrentCashSessionResponse>;
