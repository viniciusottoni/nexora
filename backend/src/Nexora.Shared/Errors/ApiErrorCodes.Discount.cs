namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro de US-054 (Desconto com autorização, <c>POST /v1/sessions/{id}/discount</c>) e
/// US-053 (Taxa de serviço com retirada registrada, <c>POST /v1/sessions/{id}/service-fee/waive</c>).
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Nem <c>percent</c> nem <c>amount</c> informados, ou valor fora do intervalo válido.</summary>
    public const string DiscountInvalidAmount = "DISCOUNT_INVALID_AMOUNT";

    /// <summary><c>scope=ITEM</c> sem <c>orderItemId</c> válido, ou item que não pertence à sessão/já cancelado.</summary>
    public const string DiscountItemNotFound = "DISCOUNT_ITEM_NOT_FOUND";

    /// <summary><c>scope=PARTIAL</c> pedido sem a sessão ter uma divisão por pessoa ativa (<c>split_people</c>).</summary>
    public const string ServiceFeePartialRequiresSplitPeople = "SERVICE_FEE_PARTIAL_REQUIRES_SPLIT_PEOPLE";
}
