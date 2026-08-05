using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.OpenCashSession;

/// <summary>Porta de <c>POST /v1/cash-sessions/open</c> (US-055 §7) — fundo de caixa informado pelo operador ao iniciar o turno.</summary>
public sealed record OpenCashSessionCommand(decimal OpeningAmount) : ICommand<CashSessionResponse>;
