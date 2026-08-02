namespace Nexora.Domain.Platform;

/// <summary>Ícone do PWA manifest (web app manifest), com o "purpose" opcional do spec.</summary>
public sealed record BrandingManifestIcon(string Src, string Sizes, string Type, string? Purpose);

/// <summary>Web App Manifest de um tenant — campos fixos do spec (start_url/scope/display/orientation nunca variam por tenant, ADR-013).</summary>
public sealed record BrandingManifest(
    string Name,
    string ShortName,
    string ThemeColor,
    string BackgroundColor,
    IReadOnlyList<BrandingManifestIcon> Icons)
{
    public const string StartUrl = "/";
    public const string Scope = "/";
    public const string Display = "standalone";
    public const string Orientation = "any";
}

/// <summary>Porta de <c>packages/domain/src/branding/manifest.ts</c> — regra pura, sem I/O.</summary>
public static class BrandingManifestFactory
{
    public static BrandingManifest Create(
        string name,
        string? shortName,
        string themeColor,
        string surfaceColor,
        IReadOnlyList<BrandingManifestIcon> icons)
    {
        var resolvedShortName = string.IsNullOrWhiteSpace(shortName) ? name : shortName.Trim();
        return new BrandingManifest(name, resolvedShortName, themeColor, surfaceColor, icons);
    }
}
