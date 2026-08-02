using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.ListTenants;

internal sealed class ListTenantsQueryHandler : IRequestHandler<ListTenantsQuery, Result<TenantListResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListTenantsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantListResponse>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantSummaryResponse(t.Id, t.Slug, t.Name, t.Plan, t.Status.ToString(), t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<TenantListResponse>.Success(new TenantListResponse(tenants));
    }
}
