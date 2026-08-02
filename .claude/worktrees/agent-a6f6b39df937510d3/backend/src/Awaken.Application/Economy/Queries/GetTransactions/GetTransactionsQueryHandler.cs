using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Economy.Queries.GetTransactions;

/// <summary>
/// US-192: combina GoldLedgerEntry e ShopOrder do usuario autenticado em uma
/// unica lista paginada ordenada por data decrescente (RN-004).
/// Projecao segura: sem dados de pagamento sensiveis (RN-001).
/// </summary>
public class GetTransactionsQueryHandler(
    IGoldWalletService walletService,
    IGoldLedgerEntryRepository ledgerRepository,
    IShopOrderRepository shopOrderRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetTransactionsQuery, TransactionPageResponse>
{
    public async Task<TransactionPageResponse> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Obtém (ou cria) a carteira para resolver o WalletId.
        var wallet = await walletService.GetOrCreateWalletAsync(userId, cancellationToken);

        // EF Core não é thread-safe: executa as queries sequencialmente.
        var ledgerResult = await ledgerRepository.GetPagedByWalletIdAsync(wallet.Id, 1, int.MaxValue, cancellationToken);
        var ordersResult = await shopOrderRepository.GetPagedByUserIdAsync(userId, 1, int.MaxValue, cancellationToken);

        var ledgerEntries = ledgerResult.Items;
        var shopOrders = ordersResult.Items;

        // Combina e ordena por data decrescente.
        var allItems = ledgerEntries
            .Select(e => new TransactionItemResponse(
                Id: e.Id,
                Type: "gold_movement",
                Description: e.Reason,
                Direction: e.Direction == GoldLedgerDirection.Credit ? "credit" : "debit",
                Amount: e.Amount,
                BalanceAfter: e.BalanceAfter,
                Channel: null,
                ProductKey: null,
                Status: null,
                CreatedAtUtc: e.CreatedAtUtc))
            .Concat(shopOrders.Select(o => new TransactionItemResponse(
                Id: o.Id,
                Type: "shop_order",
                Description: o.ProductKey,
                Direction: null,
                Amount: null,
                BalanceAfter: null,
                Channel: o.Channel,
                ProductKey: o.ProductKey,
                Status: o.Status,
                CreatedAtUtc: o.CreatedAtUtc)))
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToList();

        var totalCount = allItems.Count;
        var pagedItems = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new TransactionPageResponse(
            Items: pagedItems,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            HasMore: (page * pageSize) < totalCount);
    }
}
