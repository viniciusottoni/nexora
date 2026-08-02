namespace Nexora.Contracts.Branding;

/// <summary>Espelha <c>uploadBrandingAssetRequestSchema</c> — <c>Kind</c> é <c>LOGO_LIGHT|LOGO_DARK|FAVICON|PWA_ICON</c>.</summary>
public sealed record UploadBrandingAssetRequest(string Kind, string ContentType, int Bytes, string Sha256);

public sealed record UploadBrandingAssetResponse(Guid AssetId, string UploadUrl, string PublicUrl, DateTimeOffset ExpiresAt);
