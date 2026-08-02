using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Shop;

/// <summary>
/// US-189: pedido rastreável que orquestra toda compra, seja em Gold ou IAP.
/// RN-002: nenhuma concessão ocorre sem um ShopOrder em "pending" prévio.
/// RN-003: mesma transação/pedido não concede benefício duas vezes (ADR-010).
/// </summary>
public class ShopOrder : BaseEntity
{
    public Guid UserId { get; private set; }

    /// Canal de venda: "gold" | "iap"
    public string Channel { get; private set; } = string.Empty;

    public string ProductKey { get; private set; } = string.Empty;

    /// Status da máquina de estados: pending → granted | failed; pode ser refunded (futuro).
    public string Status { get; private set; } = "pending";

    /// ID de transação externo (RevenueCat / store) — único por registro IAP (ADR-010).
    public string? ExternalTransactionId { get; private set; }

    /// Correlação ponta-a-ponta (ADR-019 / RN-007).
    public string? CorrelationId { get; private set; }

    // Timestamps de transição de status
    public DateTime? GrantedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }

    private ShopOrder() { }

    /// Cria o pedido em estado "pending".
    public static ShopOrder Create(
        Guid userId,
        string channel,
        string productKey,
        string? externalTransactionId,
        string? correlationId,
        DateTime utcNow)
    {
        return new ShopOrder
        {
            UserId = userId,
            Channel = channel,
            ProductKey = productKey,
            Status = "pending",
            ExternalTransactionId = externalTransactionId,
            CorrelationId = correlationId,
            CreatedAtUtc = utcNow,
        };
    }

    /// Transição pending → granted.
    public void MarkGranted(DateTime utcNow)
    {
        Status = "granted";
        GrantedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// Transição pending → failed (sem débito, sem concessão — RN-005).
    public void MarkFailed(DateTime utcNow)
    {
        Status = "failed";
        FailedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// Transição granted → refunded (fluxo deferido; status previsto pela US-189).
    public void MarkRefunded(DateTime utcNow)
    {
        Status = "refunded";
        RefundedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
