using System.Text.Json.Serialization;

namespace Nexora.Contracts.Branding;

/// <summary>
/// Web App Manifest servido em <c>GET /tenant/branding.webmanifest</c> — nomes de campo em
/// snake_case porque é o spec do manifest (W3C), não convenção interna da API.
/// </summary>
public sealed record BrandingManifestIconResponse(
    string Src,
    string Sizes,
    string Type,
    [property: JsonPropertyName("purpose")] string? Purpose);

public sealed record BrandingManifestResponse(
    string Name,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("start_url")] string StartUrl,
    string Scope,
    string Display,
    string Orientation,
    [property: JsonPropertyName("theme_color")] string ThemeColor,
    [property: JsonPropertyName("background_color")] string BackgroundColor,
    IReadOnlyList<BrandingManifestIconResponse> Icons);
