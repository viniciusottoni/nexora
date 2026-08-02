using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Availability.Queries.ListUnavailableProducts;

internal sealed class ListUnavailableProductsQueryHandler
    : IRequestHandler<ListUnavailableProductsQuery, Result<UnavailableProductsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListUnavailableProductsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<UnavailableProductsResponse>> Handle(
        ListUnavailableProductsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<UnavailableProductsResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var items = await _db.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null && !p.IsAvailable)
            .OrderByDescending(p => p.UnavailableSince)
            .Select(p => new ProductAvailabilityResponse(p.Id, p.Name, p.IsAvailable, p.UnavailableReason, p.UnavailableSince))
            .ToListAsync(cancellationToken);

        return Result<UnavailableProductsResponse>.Success(new UnavailableProductsResponse(items));
    }
}
