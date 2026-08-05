namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Ações sensíveis elegíveis para elevação pontual (ADR-023) e a permissão exigida de quem
/// autoriza — porta de ACTION_PERMISSION (apps/api-edge/src/modules/auth/sensitive-authorization.service.ts).
/// </summary>
internal static class SensitiveActionCatalog
{
    public static readonly IReadOnlyDictionary<string, string> ActionPermissions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CANCEL_STARTED_ITEM"] = "order:cancel_started",
            ["DISCOUNT_ABOVE_LIMIT"] = "cash:discount_any",
            ["CLOSE_DIVERGENT_CASH"] = "cash:close_divergent",
            ["ADJUST_STOCK"] = "stock:adjust",
            ["REFUND_PAYMENT"] = "payment:refund",
            ["CLOSE_WITH_PENDING"] = "order:close_with_pending",
            // US-056 §5 (RN-011): sangria acima de operation.maxWithdrawalWithoutAuth exige
            // autorização de perfil superior — ver CashPolicy/RegisterCashMovementCommandHandler.
            ["WITHDRAWAL_ABOVE_LIMIT"] = "cash:withdraw_any",
        };
}
