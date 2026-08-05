using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.ListPlatformPlans;

internal sealed class ListPlatformPlansQueryHandler
    : IRequestHandler<ListPlatformPlansQuery, Result<PlatformPlanListResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListPlatformPlansQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PlatformPlanListResponse>> Handle(
        ListPlatformPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _db.PlatformPlans
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new { p.Code, p.Name, p.IsActive, p.CapabilitiesJson })
            .ToListAsync(cancellationToken);

        var data = plans
            .Select(p => new PlatformPlanSummaryResponse(
                p.Code,
                p.Name,
                p.IsActive,
                DeserializeCapabilities(p.CapabilitiesJson)))
            .ToList();

        return Result<PlatformPlanListResponse>.Success(new PlatformPlanListResponse(data));
    }

    private static IReadOnlyList<string> DeserializeCapabilities(string capabilitiesJson) =>
        JsonSerializer.Deserialize<List<string>>(capabilitiesJson) ?? new List<string>();
}
