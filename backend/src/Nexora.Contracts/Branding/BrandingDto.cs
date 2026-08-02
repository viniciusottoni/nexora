namespace Nexora.Contracts.Branding;

/// <summary>Espelha <c>brandingSchema</c> (pacote <c>@db/contracts</c>) do NestJS original — serializado dentro de <c>TenantConfig.Branding</c> (JSONB).</summary>
public sealed record BrandingColorsDto(string Primary, string Secondary, string Surface, string OnPrimary);

public sealed record BrandingLogoDto(string? Light, string? Dark);

public sealed record BrandingFontsDto(string Body, string Display);

public sealed record BrandingTextsDto(string Welcome, string OrderConfirmed, string Thanks, string Terms);

public sealed record BrandingPwaIconDto(string Src, string Sizes, string Type, string? Purpose);

public sealed record BrandingPwaDto(string Name, string ShortName, string ThemeColor, IReadOnlyList<BrandingPwaIconDto> Icons);

public sealed record BrandingDto(
    BrandingColorsDto Colors,
    BrandingLogoDto Logo,
    string? Favicon,
    BrandingFontsDto Fonts,
    int Radius,
    BrandingTextsDto Texts,
    BrandingPwaDto Pwa);
