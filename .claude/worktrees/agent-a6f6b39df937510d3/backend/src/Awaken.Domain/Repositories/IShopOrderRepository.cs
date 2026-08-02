using Awaken.Domain.Common;
using Awaken.Domain.Entities.Shop;

namespace Awaken.Domain.Repositories;

/// US-189: repositório de pedidos de compra — canal Gold e IAP.
public interface IShopOrderRepository : IRepository<ShopOrder>
{
    /// Busca pedido por ExternalTransactionId para idempotência IAP (ADR-010).
    Task<ShopOrder?> GetByExternalTransactionIdAsync(string externalTransactionId, CancellationToken ct = default);

    /// Retorna pedidos de um usuário em ordem de criação decrescente.
    Task<IReadOnlyList<ShopOrder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// US-192: retorna pedidos de compra de um usuario paginados, ordem desc.
    /// </summary>
    Task<(IReadOnlyList<ShopOrder> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// US-193: listagem paginada administrativa com filtros opcionais.
    /// Ordenação por CreatedAtUtc descendente.
    Task<(IReadOnlyList<ShopOrder> Items, int TotalCount)> GetPagedByFilterAsync(
        Guid? userId,
        string? status,
        string? channel,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
