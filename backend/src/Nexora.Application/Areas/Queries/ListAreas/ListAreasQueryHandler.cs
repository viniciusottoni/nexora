using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Areas.Queries.ListAreas;

internal sealed class ListAreasQueryHandler : IRequestHandler<ListAreasQuery, Result<AreaListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListAreasQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AreaListResponse>> Handle(ListAreasQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<AreaListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var items = await _db.Areas
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.DeletedAt == null)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .Select(a => new AreaResponse(
                a.Id,
                a.Name,
                a.SortOrder,
                a.IsActive,
                _db.DiningTables.Count(t => t.AreaId == a.Id && t.DeletedAt == null)))
            .ToListAsync(cancellationToken);

        return Result<AreaListResponse>.Success(new AreaListResponse(items));
    }
}
