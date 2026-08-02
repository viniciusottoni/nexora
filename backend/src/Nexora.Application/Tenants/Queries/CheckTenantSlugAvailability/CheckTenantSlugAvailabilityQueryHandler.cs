using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.CheckTenantSlugAvailability;

internal sealed class CheckTenantSlugAvailabilityQueryHandler
    : IRequestHandler<CheckTenantSlugAvailabilityQuery, Result<SlugAvailabilityResponse>>
{
    private readonly IApplicationDbContext _db;

    public CheckTenantSlugAvailabilityQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SlugAvailabilityResponse>> Handle(
        CheckTenantSlugAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var taken = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Slug == request.Slug && t.DeletedAt == null, cancellationToken);

        return Result<SlugAvailabilityResponse>.Success(new SlugAvailabilityResponse(request.Slug, !taken));
    }
}
