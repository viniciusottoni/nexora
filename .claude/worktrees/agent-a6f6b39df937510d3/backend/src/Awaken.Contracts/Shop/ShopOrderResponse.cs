namespace Awaken.Contracts.Shop;

/// US-189: resposta da orquestração de compra (Gold ou IAP).
/// RN-007: CorrelationId sempre presente na resposta.
public record ShopOrderResponse(
    Guid OrderId,
    string Channel,
    string ProductKey,
    string Status,
    string? CorrelationId);
