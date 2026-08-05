namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro de US-052 (Múltiplas formas de pagamento) e US-058 (Pagamento de maquininha externa) — <c>POST /v1/sessions/{id}/payments</c>.</summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// US-052 §4, cenário "Soma divergente do total": a soma de <c>payments[].amount</c> não bate
    /// com o total da conta. <c>meta</c> traz <c>{ total, provided, difference }</c>.
    /// </summary>
    public const string PaymentSumMismatch = "PAYMENT_SUM_MISMATCH";

    /// <summary>Forma de pagamento (<c>method</c>) informada não é reconhecida.</summary>
    public const string PaymentMethodInvalid = "PAYMENT_METHOD_INVALID";

    /// <summary>
    /// US-058 §4, cenário "Referência duplicada": o mesmo <c>provider</c>/<c>providerRef</c> já foi
    /// registrado no turno e o chamador não confirmou explicitamente (<c>confirmDuplicate</c>).
    /// </summary>
    public const string PaymentDuplicateReference = "PAYMENT_DUPLICATE_REFERENCE";

    /// <summary><c>POST /v1/sessions/{id}/payments</c> pedido para uma sessão já paga/fechada.</summary>
    public const string PaymentSessionNotPayable = "PAYMENT_SESSION_NOT_PAYABLE";

    /// <summary><c>payments</c> vazio — nenhuma forma de pagamento informada.</summary>
    public const string PaymentListEmpty = "PAYMENT_LIST_EMPTY";
}
