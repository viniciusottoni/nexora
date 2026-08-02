using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Queries.ListProducts;

internal sealed class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, Result<ProductListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListProductsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductListResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ProductListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var query = _db.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null);

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var items = await query
            .OrderBy(p => p.CategoryId)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new ProductResponse(
                p.Id,
                p.CategoryId,
                p.Category.Name,
                p.StationId,
                p.StationId == null ? null : _db.Stations.Where(s => s.Id == p.StationId).Select(s => s.Name).FirstOrDefault(),
                p.Name,
                p.Description,
                p.IngredientsText,
                p.Allergens,
                _db.MediaAssets
                    .Where(m => m.OwnerType == "PRODUCT" && m.OwnerId == p.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Url)
                    .FirstOrDefault(),
                p.SortOrder,
                p.IsActive,
                p.IsAvailable,
                p.AllowsFractions,
                p.MaxFractions))
            .ToListAsync(cancellationToken);

        return Result<ProductListResponse>.Success(new ProductListResponse(items));
    }
}
