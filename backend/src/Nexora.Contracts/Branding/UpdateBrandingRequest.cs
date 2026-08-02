namespace Nexora.Contracts.Branding;

/// <summary>Patches parciais — campo ausente (<c>null</c>) significa "não alterar", espelha <c>updateBrandingRequestSchema</c>.</summary>
public sealed record BrandingColorsPatch(string? Primary, string? Secondary, string? Surface, string? OnPrimary);

public sealed record BrandingFontsPatch(string? Body, string? Display);

public sealed record BrandingTextsPatch(string? Welcome, string? OrderConfirmed, string? Thanks, string? Terms);

public sealed record BrandingPwaPatch(string? Name, string? ShortName, string? ThemeColor, IReadOnlyList<BrandingPwaIconDto>? Icons);

public sealed record UpdateBrandingRequest(
    BrandingColorsPatch? Colors,
    BrandingLogoDto? Logo,
    string? Favicon,
    BrandingFontsPatch? Fonts,
    int? Radius,
    BrandingTextsPatch? Texts,
    BrandingPwaPatch? Pwa);
