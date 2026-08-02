using Awaken.Contracts.Admin.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldLedger;

/// <summary>
/// US-229: projeção paginada de GoldLedgerEntry com filtros admin.
/// Traduz UserId → WalletId quando userId informado.
/// </summary>
public class GetAdminGoldLedgerQueryHandler(
    IGoldWalletRepository goldWalletRepository,
    IGoldLedgerEntryRepository goldLedgerEntryRepository)
    : IRequestHandler<GetAdminGoldLedgerQuery, GoldLedgerPageResponse>
{
    public async Task<GoldLedgerPageResponse> Handle(
        GetAdminGoldLedgerQuery request, CancellationToken ct)
    {
        Guid? walletId = null;
        Guid? resolvedUserId = null;

        if (request.UserId.HasValue)
        {
            var wallet = await goldWalletRepository.GetByUserIdAsync(request.UserId.Value, ct);
            if (wallet is null) return new GoldLedgerPageResponse([], 0, request.Page, request.PageSize);
            walletId        = wallet.Id;
            resolvedUserId  = wallet.UserId;
        }

        var (entries, total) = await goldLedgerEntryRepository.GetAdminPagedAsync(
            walletId, request.Direction, request.DateFrom, request.DateTo,
            request.Page, request.PageSize, ct);

        // Quando userId não foi filtrado, precisamos buscar o UserId de cada carteira em memória.
        // Para o MVP (volume baixo), isso é aceitável; para escala, migrar para JOIN em query.
        Dictionary<Guid, Guid>? walletUserMap = null;
        if (!request.UserId.HasValue && entries.Count > 0)
        {
            var allWallets = await goldWalletRepository.GetAllAsync(ct);
            walletUserMap = allWallets.ToDictionary(w => w.Id, w => w.UserId);
        }

        Guid GetUserId(Guid wId) =>
            resolvedUserId ?? (walletUserMap?.GetValueOrDefault(wId) ?? Guid.Empty);

        var items = entries
            .Select(e => new GoldLedgerEntryAdminResponse(
                e.Id,
                e.WalletId,
                GetUserId(e.WalletId),
                e.Direction == GoldLedgerDirection.Credit ? "credit" : "debit",
                e.Amount,
                e.Reason,
                e.ReferenceType,
                e.ReferenceId,
                e.BalanceAfter,
                e.CorrelationId,
                e.CreatedAtUtc))
            .ToList();

        return new GoldLedgerPageResponse(items, total, request.Page, request.PageSize);
    }
}
