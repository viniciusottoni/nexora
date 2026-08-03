using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.RegisterPartialPayment;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/bill/partial-payment</c> (US-027 §4, cenário "Divisão por
/// valor": "alguém pagar R$ 50,00... devem restar R$ 130,00 em aberto... a sessão deve permanecer em
/// BILL_REQUESTED"). Diferente da atribuição por item/retirada de taxa, este é um FATO de negócio de
/// verdade — cria um <see cref="Nexora.Domain.Cashier.Payment"/> vinculado à sessão (não um cálculo
/// efêmero) — mas não fecha nem encerra a comanda: isso é US-052, fora desta história.
/// </summary>
public sealed record RegisterPartialPaymentCommand(Guid SessionId, decimal Amount, string Method) : ICommand<PartialPaymentResponse>;
