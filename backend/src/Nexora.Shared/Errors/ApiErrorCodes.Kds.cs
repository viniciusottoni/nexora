namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do KDS Cozinha (E-04, ADR-021) — avanço por teclado numérico e desfazer.</summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// US-041 §7: código curto digitado no teclado numérico não corresponde a nenhum pedido com
    /// item ativo na praça informada — valor literal idêntico ao contrato de API da história
    /// (<c>{ "code": "SHORT_CODE_NOT_FOUND" }</c>).
    /// </summary>
    public const string KdsShortCodeNotFound = "SHORT_CODE_NOT_FOUND";

    /// <summary>Pedido encontrado, mas nenhum item seu na praça informada está em estado que ainda pode avançar (todos já Served/Cancelled).</summary>
    public const string KdsNoEligibleItem = "KDS_NO_ELIGIBLE_ITEM";

    /// <summary>US-041 §3 ("Desfazer avanço acidental"): a janela de 10 s desde a última transição já passou.</summary>
    public const string KdsUndoWindowExpired = "KDS_UNDO_WINDOW_EXPIRED";
}
