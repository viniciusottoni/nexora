using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Categories.Queries.ListCategories;

internal sealed class ListCategoriesQueryHandler : IRequestHandler<ListCategoriesQuery, Result<CategoryListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListCategoriesQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CategoryListResponse>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<CategoryListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var items = await _db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.DeletedAt == null)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.Description,
                c.SortOrder,
                c.IsActive,
                _db.Products.Count(p => p.CategoryId == c.Id && p.DeletedAt == null)))
            .ToListAsync(cancellationToken);

        return Result<CategoryListResponse>.Success(new CategoryListResponse(items));
    }
}
