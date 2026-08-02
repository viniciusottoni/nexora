using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Branding;
using Nexora.Contracts.Branding;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Branding.Queries.GetBrandingManifest;

internal sealed class GetBrandingManifestQueryHandler : IRequestHandler<GetBrandingManifestQuery, Result<BrandingManifestResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetBrandingManifestQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BrandingManifestResponse>> Handle(GetBrandingManifestQuery request, CancellationToken cancellationToken)
    {
        var host = BrandingHost.Normalize(request.Host);

        var record = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Domain != null && t.Domain.ToLower() == host)
            .Select(t => new
            {
                t.Name,
                Branding = _db.TenantConfigs.Where(c => c.TenantId == t.Id).Select(c => c.Branding).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (record?.Branding is null)
        {
            return Result<BrandingManifestResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.BrandingTenantNotFound);
        }

        var branding = BrandingDefaults.Parse(record.Branding, record.Name);

        var icons = branding.Pwa.Icons
            .Select(icon => new BrandingManifestIcon(icon.Src, icon.Sizes, icon.Type, icon.Purpose))
            .ToList();

        var manifest = BrandingManifestFactory.Create(
            name: string.IsNullOrWhiteSpace(branding.Pwa.Name) ? record.Name : branding.Pwa.Name,
            shortName: branding.Pwa.ShortName,
            themeColor: branding.Pwa.ThemeColor,
            surfaceColor: branding.Colors.Surface,
            icons: icons);

        var response = new BrandingManifestResponse(
            manifest.Name,
            manifest.ShortName,
            BrandingManifest.StartUrl,
            BrandingManifest.Scope,
            BrandingManifest.Display,
            BrandingManifest.Orientation,
            manifest.ThemeColor,
            manifest.BackgroundColor,
            manifest.Icons.Select(i => new BrandingManifestIconResponse(i.Src, i.Sizes, i.Type, i.Purpose)).ToList());

        return Result<BrandingManifestResponse>.Success(response);
    }
}
