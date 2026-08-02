using Awaken.Contracts.Admin.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldWalletDetail;

/// <summary>
/// US-229: retorna carteira + últimos 50 lançamentos para o detalhe admin.
/// Retorna null quando carteira inexistente (RN-001: wallet pode não existir ainda).
/// </summary>
public class GetAdminGoldWalletDetailQueryHandler(
    IGoldWalletRepository goldWalletRepository,
    IGoldLedgerEntryRepository goldLedgerEntryRepository)
    : IRequestHandler<GetAdminGoldWalletDetailQuery, GoldWalletAdminResponse?>
{
    private const int RecentLedgerSize = 50;

    public async Task<GoldWalletAdminResponse?> Handle(
        GetAdminGoldWalletDetailQuery request, CancellationToken ct)
    {
        var wallet = await goldWalletRepository.GetByUserIdAsync(request.UserId, ct);
        if (wallet is null) return null;

        var (entries, total) = await goldLedgerEntryRepository.GetPagedByWalletIdAsync(
            wallet.Id, 1, RecentLedgerSize, ct);

        var recentLedger = entries
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

        return new GoldWalletAdminResponse(
            wallet.Id,
            wallet.UserId,
            wallet.Balance,
            wallet.CreatedAtUtc,
            recentLedger,
            total);
    }
}
