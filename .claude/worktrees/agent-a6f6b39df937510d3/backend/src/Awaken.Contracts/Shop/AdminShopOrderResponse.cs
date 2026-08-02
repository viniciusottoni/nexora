namespace Awaken.Contracts.Shop;

/// <summary>
/// US-193: projeção administrativa de pedido de compra — somente leitura.
/// RN-003: sem token nem dado de pagamento (ADR-015).
/// RN-005: CorrelationId presente para cruzamento com auditoria.
/// </summary>
public record AdminShopOrderResponse(
    Guid OrderId,
    Guid UserId,
    string Channel,
    string ProductKey,
    string Status,
    string? ExternalTransactionId,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? GrantedAtUtc,
    DateTime? FailedAtUtc);
