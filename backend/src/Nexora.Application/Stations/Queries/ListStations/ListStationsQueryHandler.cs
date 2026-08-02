using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Stations;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Stations.Queries.ListStations;

internal sealed class ListStationsQueryHandler : IRequestHandler<ListStationsQuery, Result<StationListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListStationsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StationListResponse>> Handle(ListStationsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<StationListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        // Contagem de produtos vinculados por praça exibida na listagem (US-017 §10).
        var items = await _db.Stations
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.DeletedAt == null)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Select(s => new StationResponse(
                s.Id,
                s.Code,
                s.Name,
                s.Color,
                s.CapacitySlots,
                s.IsBottleneck,
                s.SortOrder,
                s.IsActive,
                _db.Products.Count(p => p.StationId == s.Id && p.DeletedAt == null)))
            .ToListAsync(cancellationToken);

        return Result<StationListResponse>.Success(new StationListResponse(items));
    }
}
