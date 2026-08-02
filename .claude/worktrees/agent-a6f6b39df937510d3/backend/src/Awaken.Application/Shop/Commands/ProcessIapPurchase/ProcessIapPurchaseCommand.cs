using Awaken.Contracts.Shop;
using MediatR;

namespace Awaken.Application.Shop.Commands.ProcessIapPurchase;

/// US-226: o app envia apenas a referência da transação e o produto — nunca
/// quantidade de Gold (RN-001). A resposta (<see cref="ShopOrderResponse"/>) dá
/// ao app o status seguro do pedido (pending/granted/failed) para que ele saiba
/// tratar compra pendente, aprovada, negada, já processada e erro temporário
/// (US-226 seção 10).
public record ProcessIapPurchaseCommand(
    Guid UserId,
    string TransactionId,
    string ProductKey,
    string Store) : IRequest<ShopOrderResponse>;
