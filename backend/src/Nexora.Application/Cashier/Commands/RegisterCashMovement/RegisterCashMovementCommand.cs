using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.RegisterCashMovement;

/// <summary>
/// Porta de <c>POST /v1/cash-sessions/movements</c> (US-056 §7) — <see cref="Type"/> é <c>WITHDRAWAL</c>
/// (sangria) ou <c>SUPPLY</c> (suprimento), sempre lançado na sessão de caixa ABERTA do operador
/// corrente na loja (não recebe <c>cashSessionId</c> no corpo — resolvido pelo contexto, mesma
/// convenção de <c>GetCurrentCashSessionQuery</c>). <see cref="AuthorizationToken"/> (ADR-023) só é
/// relevante quando <see cref="Type"/> é <c>WITHDRAWAL</c> e <see cref="Amount"/> ultrapassa
/// <c>operation.maxWithdrawalWithoutAuth</c> (US-056 §4, cenário "Sangria acima do limite").
/// </summary>
public sealed record RegisterCashMovementCommand(
    string Type,
    decimal Amount,
    string Reason,
    string? AuthorizationToken) : ICommand<RegisterCashMovementResponse>;
