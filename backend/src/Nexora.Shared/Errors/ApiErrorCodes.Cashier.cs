namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de caixa — abertura/fechamento (US-055) e sangria/suprimento (US-056), ADR-021.</summary>
public static partial class ApiErrorCodes
{
    public const string CashSessionNotFound = "CASH_SESSION_NOT_FOUND";

    /// <summary>
    /// US-055 §4, cenário "Um caixa por operador e turno": <c>uq_cash_open</c> (store_id, operator_id,
    /// filtrado por status &lt;&gt; CLOSED) já tem uma sessão aberta para este operador — 409 com
    /// <c>meta.sessionId</c> apontando a sessão existente (mesma convenção de <see cref="TableAlreadyOpen"/>).
    /// </summary>
    public const string CashSessionAlreadyOpen = "CASH_SESSION_ALREADY_OPEN";

    /// <summary>Fechamento pedido para uma sessão que já está em <c>CLOSED</c>.</summary>
    public const string CashSessionAlreadyClosed = "CASH_SESSION_ALREADY_CLOSED";

    /// <summary>
    /// <c>GET /v1/cash-sessions/current</c> e <c>POST /v1/cash-sessions/movements</c>: não existe
    /// sessão de caixa aberta para o operador corrente na loja — US-056 §4, cenário "Movimento sem
    /// caixa aberto".
    /// </summary>
    public const string NoOpenCashSession = "NO_OPEN_CASH_SESSION";

    /// <summary>
    /// US-055 §4/§7, cenário "Mesa aberta no fechamento" (RN-018): existe sessão de mesa ainda não
    /// encerrada na loja e nenhum <c>X-Authorization-Token</c> válido para <c>CLOSE_DIVERGENT_CASH</c>
    /// foi apresentado. <c>meta.openSessions</c> traz <c>{ table, total }</c> de cada mesa aberta.
    /// </summary>
    public const string OpenTables = "OPEN_TABLES";

    /// <summary>
    /// US-055 §4, cenário "Divergência no fechamento": <c>|divergência| &gt; limiar configurado</c>
    /// (<c>CashPolicy.ResolveDivergenceJustificationThreshold</c>) e nenhuma <c>justification</c> foi
    /// informada no corpo do fechamento.
    /// </summary>
    public const string CashJustificationRequired = "CASH_JUSTIFICATION_REQUIRED";

    /// <summary>Tipo de movimento de caixa fora do vocabulário fechado <c>WITHDRAWAL</c>/<c>SUPPLY</c>.</summary>
    public const string CashMovementTypeInvalid = "CASH_MOVEMENT_TYPE_INVALID";
}
