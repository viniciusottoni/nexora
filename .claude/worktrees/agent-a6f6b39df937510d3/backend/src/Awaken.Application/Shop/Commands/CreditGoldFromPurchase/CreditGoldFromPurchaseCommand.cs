using MediatR;

namespace Awaken.Application.Shop.Commands.CreditGoldFromPurchase;

/// <summary>
/// US-226: comando dedicado para creditar Gold comprado com dinheiro real.
///
/// RN-001/RN-002: a quantidade (<see cref="Amount"/>) jamais vem do app — quem
/// monta este comando (ProcessIapPurchaseCommandHandler) resolve esse valor
/// exclusivamente a partir de ShopProduct.GoldAmount, lido do catálogo
/// server-side. Este comando não aceita nenhum dado vindo diretamente do payload
/// do cliente.
///
/// RN-003/RN-004: a idempotência por transação externa já é garantida antes
/// deste comando ser despachado (ExternalTransactionId único em ShopOrder); este
/// comando não duplica essa checagem, apenas credita o ShopOrder já validado.
/// </summary>
public record CreditGoldFromPurchaseCommand(
    Guid UserId,
    long Amount,
    Guid ShopOrderId) : IRequest<Unit>;
