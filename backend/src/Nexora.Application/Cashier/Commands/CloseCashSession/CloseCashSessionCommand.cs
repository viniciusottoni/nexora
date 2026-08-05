using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.CloseCashSession;

/// <summary>
/// Porta de <c>POST /v1/cash-sessions/{id}/close</c> (US-055 §7). <see cref="AuthorizationToken"/>
/// (header <c>X-Authorization-Token</c>, ADR-023) só é relevante quando existe mesa ainda aberta na
/// loja (RN-018) — ver <c>Cashier.Support.CashCloseGuard</c>.
/// </summary>
public sealed record CloseCashSessionCommand(
    Guid SessionId,
    decimal CountedAmount,
    string? Justification,
    string? AuthorizationToken) : ICommand<CloseCashSessionResponse>;
