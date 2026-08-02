using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Branding;
using Nexora.Contracts.Branding;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Branding.Queries.GetPublicBranding;

internal sealed class GetPublicBrandingQueryHandler : IRequestHandler<GetPublicBrandingQuery, Result<BrandingResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicBrandingQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BrandingResponse>> Handle(GetPublicBrandingQuery request, CancellationToken cancellationToken)
    {
        var host = BrandingHost.Normalize(request.Host);

        var record = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Domain != null && t.Domain.ToLower() == host)
            .Select(t => new
            {
                t.Id,
                t.Name,
                Config = _db.TenantConfigs.Where(c => c.TenantId == t.Id).Select(c => new { c.Branding, c.BrandingVersion }).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (record?.Config is null)
        {
            return Result<BrandingResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.BrandingTenantNotFound);
        }

        var branding = BrandingDefaults.Parse(record.Config.Branding, record.Name);

        return Result<BrandingResponse>.Success(new BrandingResponse(
            new TenantBrandingInfoResponse(record.Id, record.Name),
            branding,
            record.Config.BrandingVersion));
    }
}
