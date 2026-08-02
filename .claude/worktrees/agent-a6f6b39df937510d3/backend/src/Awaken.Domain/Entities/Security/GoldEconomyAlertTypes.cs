namespace Awaken.Domain.Entities.Security;

/// <summary>
/// US-228: tipos de alerta (SecurityAlert.AlertType) emitidos pela reconciliação
/// automática da economia Gold (GoldWallet, GoldLedgerEntry, ShopOrder, InventoryItem).
/// SecurityAlert.AlertType é uma string livre (ver SecurityAlert.cs) — estas constantes
/// apenas mapeiam 1:1 com a lista mínima de tipos de alerta da seção 7 da US-228.
/// </summary>
public static class GoldEconomyAlertTypes
{
    /// RN-001: saldo da carteira (GoldWallet.Balance) não bate com o último BalanceAfter do ledger.
    public const string BalanceMismatch = "gold_balance_mismatch";

    /// Defensivo: GoldWallet.Balance ficou negativo (não deveria ser possível via GoldWallet.Debit).
    public const string NegativeBalance = "gold_negative_balance";

    /// Carteira existe mas não possui nenhum GoldLedgerEntry (saldo não rastreável).
    public const string LedgerMissing = "gold_ledger_missing";

    /// RN-002: ShopOrder Channel="gold" Status="granted" sem GoldLedgerEntry de débito correspondente.
    public const string OrderGrantedWithoutDebit = "gold_order_without_debit";

    /// RN-003: GoldLedgerEntry de crédito referenciando um ShopOrder que não existe ou não está "granted".
    public const string CreditWithoutValidation = "gold_credit_without_validation";

    /// RN-004: InventoryItem com Quantity > 0 sem origem rastreável (best-effort — ver handler).
    public const string ItemWithoutOrigin = "gold_item_without_origin";

    /// Heurística defensiva: duas ShopOrder com mesmo ProductKey+UserId, ambas granted, criadas a poucos segundos.
    public const string DuplicatePurchase = "gold_duplicate_purchase";

    /// Volume de pedidos de compra (qualquer canal) por usuário acima do limiar nas últimas 24h.
    public const string AbnormalVolume = "gold_abnormal_volume";

    /// Muitas falhas de compra (Status="failed") por usuário em curto período.
    public const string ExcessiveFailures = "gold_excessive_failures";

    /// US-229: todos os tipos de alerta emitidos pela reconciliação, para filtros admin.
    public static readonly IReadOnlyList<string> All =
    [
        BalanceMismatch,
        NegativeBalance,
        LedgerMissing,
        OrderGrantedWithoutDebit,
        CreditWithoutValidation,
        ItemWithoutOrigin,
        DuplicatePurchase,
        AbnormalVolume,
        ExcessiveFailures,
    ];
}
