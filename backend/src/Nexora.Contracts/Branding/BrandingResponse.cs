namespace Nexora.Contracts.Branding;

public sealed record TenantBrandingInfoResponse(Guid Id, string Name);

public sealed record BrandingResponse(TenantBrandingInfoResponse Tenant, BrandingDto Branding, int ConfigVersion);

public sealed record BrandingContrastIssueResponse(string Pair, double Ratio, string Suggested);

public sealed record BrandingContrastResponse(bool Valid, double MinimumRatio, IReadOnlyList<BrandingContrastIssueResponse> Issues);

public sealed record UpdateBrandingResponse(
    TenantBrandingInfoResponse Tenant,
    BrandingDto Branding,
    int ConfigVersion,
    BrandingContrastResponse Contrast);
