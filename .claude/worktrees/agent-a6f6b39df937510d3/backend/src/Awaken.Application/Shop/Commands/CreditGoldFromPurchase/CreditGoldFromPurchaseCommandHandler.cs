using Awaken.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Shop.Commands.CreditGoldFromPurchase;

/// <summary>
/// US-226: credita Gold comprado com dinheiro real na carteira do usuário.
///
/// Responsabilidade isolada (US-226 seção 9): separar "creditar Gold" de
/// "conceder item de inventário" torna o fluxo de compra de pacote de Gold
/// testável isoladamente e documenta a intenção — este comando nunca decide
/// quantidade, apenas executa o crédito de um valor já resolvido pelo chamador
/// a partir do catálogo server-side (RN-001/RN-002).
///
/// RN-007: o crédito em si (GoldWallet + GoldLedgerEntry + audit log) é
/// responsabilidade de IGoldWalletService.CreditAsync, já auditado
/// (AuditActions.GoldWalletCredited) — este handler não duplica auditoria.
/// </summary>
public class CreditGoldFromPurchaseCommandHandler(
    IGoldWalletService goldWalletService,
    ILogger<CreditGoldFromPurchaseCommandHandler> logger) : IRequestHandler<CreditGoldFromPurchaseCommand, Unit>
{
    public async Task<Unit> Handle(CreditGoldFromPurchaseCommand request, CancellationToken cancellationToken)
    {
        await goldWalletService.CreditAsync(
            request.UserId,
            request.Amount,
            reason: "gold_purchase",
            referenceType: "shop_order",
            referenceId: request.ShopOrderId.ToString(),
            cancellationToken);

        logger.LogInformation(
            "Gold creditado via compra: usuário {UserId} ShopOrder {ShopOrderId}",
            request.UserId, request.ShopOrderId);

        return Unit.Value;
    }
}
