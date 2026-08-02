using Awaken.Contracts.Shop;
using MediatR;

namespace Awaken.Application.Shop.Commands.PurchaseWithGold;

/// <summary>
/// US-189: compra de item via Gold.
/// Cria ShopOrder, valida saldo, debita carteira, concede item via inventário.
///
/// US-227 / RN-003: IdempotencyKey opcional (header Idempotency-Key) evita
/// concessão dupla em reenvio da mesma requisição — quando informado e já
/// existir um ShopOrder com essa chave, o pedido existente é retornado sem
/// novo débito ou concessão.
/// </summary>
public record PurchaseWithGoldCommand(string ItemKey, string? IdempotencyKey = null) : IRequest<ShopOrderResponse>;
