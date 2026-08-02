using Awaken.Contracts.Admin.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldOrderDetail;

/// <summary>
/// US-229: carrega o pedido e os lançamentos de ledger que o referenciam.
/// RN-003: ExternalTransactionId excluído da resposta.
/// </summary>
public class GetAdminGoldOrderDetailQueryHandler(
    IShopOrderRepository shopOrderRepository,
    IGoldWalletRepository goldWalletRepository,
    IGoldLedgerEntryRepository goldLedgerEntryRepository)
    : IRequestHandler<GetAdminGoldOrderDetailQuery, GoldOrderDetailAdminResponse?>
{
    public async Task<GoldOrderDetailAdminResponse?> Handle(
        GetAdminGoldOrderDetailQuery request, CancellationToken ct)
    {
        var order = await shopOrderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null) return null;

        // Lançamentos de ledger que referenciam este pedido
        var wallet = await goldWalletRepository.GetByUserIdAsync(order.UserId, ct);

        IReadOnlyList<GoldLedgerEntryAdminResponse> relatedLedger = [];

        if (wallet is not null)
        {
            var (allEntries, _) = await goldLedgerEntryRepository.GetAdminPagedAsync(
                wallet.Id, null, null, null, 1, 200, ct);

            var orderId = order.Id.ToString();
            relatedLedger = allEntries
                .Where(e => e.ReferenceType == "shop_order" && e.ReferenceId == orderId)
                .Select(e => new GoldLedgerEntryAdminResponse(
                    e.Id,
                    e.WalletId,
                    wallet.UserId,
                    e.Direction == GoldLedgerDirection.Credit ? "credit" : "debit",
                    e.Amount,
                    e.Reason,
                    e.ReferenceType,
                    e.ReferenceId,
                    e.BalanceAfter,
                    e.CorrelationId,
                    e.CreatedAtUtc))
                .ToList();
        }

        return new GoldOrderDetailAdminResponse(
            order.Id,
            order.UserId,
            order.Channel,
            order.ProductKey,
            order.Status,
            order.CorrelationId,
            order.CreatedAtUtc,
            order.GrantedAtUtc,
            order.FailedAtUtc,
            relatedLedger);
    }
}
