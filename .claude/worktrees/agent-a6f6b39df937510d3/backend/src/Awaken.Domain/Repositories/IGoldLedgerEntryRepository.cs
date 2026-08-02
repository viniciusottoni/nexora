using Awaken.Domain.Common;
using Awaken.Domain.Entities.Economy;

namespace Awaken.Domain.Repositories;

public interface IGoldLedgerEntryRepository : IRepository<GoldLedgerEntry>
{
    Task<IReadOnlyList<GoldLedgerEntry>> GetByWalletIdAsync(
        Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>
    /// US-192: retorna lancamentos de Gold de uma carteira paginados,
    /// ordenados por data decrescente.
    /// </summary>
    Task<(IReadOnlyList<GoldLedgerEntry> Items, int TotalCount)> GetPagedByWalletIdAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// US-229: listagem admin paginada com filtros opcionais (carteira, direção, período).
    /// Ordenação por CreatedAtUtc descendente.
    /// </summary>
    Task<(IReadOnlyList<GoldLedgerEntry> Items, int TotalCount)> GetAdminPagedAsync(
        Guid? walletId,
        string? direction,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
