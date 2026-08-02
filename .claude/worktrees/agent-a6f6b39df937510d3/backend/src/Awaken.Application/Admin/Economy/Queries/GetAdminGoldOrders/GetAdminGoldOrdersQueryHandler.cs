using Awaken.Contracts.Admin.Economy;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldOrders;

/// <summary>
/// US-229: sempre filtra channel="gold"; ProductKey é aplicado em memória
/// (GetPagedByFilterAsync não suporta filtro de ProductKey nativamente).
/// RN-003: ExternalTransactionId excluído da resposta.
/// </summary>
public class GetAdminGoldOrdersQueryHandler(
    IShopOrderRepository shopOrderRepository)
    : IRequestHandler<GetAdminGoldOrdersQuery, GoldOrderPageResponse>
{
    private const int MaxRows = 50_000;

    public async Task<GoldOrderPageResponse> Handle(
        GetAdminGoldOrdersQuery request, CancellationToken ct)
    {
        var (rawItems, _) = await shopOrderRepository.GetPagedByFilterAsync(
            request.UserId,
            request.Status,
            "gold",
            request.DateFrom,
            request.DateTo,
            1,
            MaxRows,
            ct);

        // Filtro de ProductKey (in-memory para MVP)
        var filtered = string.IsNullOrEmpty(request.ProductKey)
            ? rawItems.ToList()
            : rawItems.Where(o => o.ProductKey.Contains(request.ProductKey, StringComparison.OrdinalIgnoreCase)).ToList();

        var total = filtered.Count;
        var paged = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = paged
            .Select(o => new GoldOrderAdminResponse(
                o.Id,
                o.UserId,
                o.Channel,
                o.ProductKey,
                o.Status,
                o.CorrelationId,
                o.CreatedAtUtc,
                o.GrantedAtUtc,
                o.FailedAtUtc))
            .ToList();

        return new GoldOrderPageResponse(items, total, request.Page, request.PageSize);
    }
}
