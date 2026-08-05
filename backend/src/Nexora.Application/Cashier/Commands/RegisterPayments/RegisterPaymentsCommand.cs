using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.RegisterPayments;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/payments</c> (US-052, Múltiplas formas de pagamento na mesma
/// conta; e US-058, Registrar pagamento de maquininha externa — mesma chamada, cada item de
/// <see cref="Payments"/> pode carregar <c>provider</c>/<c>providerRef</c>/<c>brand</c>/
/// <c>installments</c> quando vem de uma maquininha). A soma de <see cref="Payments"/>[].Amount
/// precisa ser EXATAMENTE igual ao total da conta (ADR-017) — divergência é 422
/// <c>PAYMENT_SUM_MISMATCH</c>, nunca arredondada silenciosamente.
/// </summary>
public sealed record RegisterPaymentsCommand(
    Guid SessionId,
    IReadOnlyList<PaymentInput> Payments,
    DateTimeOffset? OccurredAt = null) : ICommand<RegisterPaymentsResponse>;

/// <summary>Ver <see cref="PaymentRequest"/> — mesmos campos, projetados para o comando (Application não referencia Contracts diretamente na direção contrária).</summary>
public sealed record PaymentInput(
    string Method,
    decimal Amount,
    decimal? ReceivedAmount,
    string? Provider,
    string? ProviderRef,
    string? Brand,
    int Installments,
    bool ConfirmDuplicate);
