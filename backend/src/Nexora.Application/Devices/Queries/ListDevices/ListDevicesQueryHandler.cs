using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices; // DeviceKindMapper
using Nexora.Contracts.Devices;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Devices.Queries.ListDevices;

internal sealed class ListDevicesQueryHandler : IRequestHandler<ListDevicesQuery, Result<DeviceListResponse>>
{
    // STALE_DEVICE_MS em device-registry.ts.
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListDevicesQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<DeviceListResponse>> Handle(ListDevicesQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<DeviceListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var now = DateTimeOffset.UtcNow;

        var devices = await _db.Devices
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.DeletedAt == null)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = devices
            .Select(d => new DeviceResponse(
                d.Id,
                d.Label,
                DeviceKindMapper.ToKindCode(d.Type),
                d.IsActive,
                d.LastSeenAt,
                d.LastSeenAt is not null && now - d.LastSeenAt.Value > StaleThreshold))
            .ToList();

        return Result<DeviceListResponse>.Success(new DeviceListResponse(items));
    }
}
