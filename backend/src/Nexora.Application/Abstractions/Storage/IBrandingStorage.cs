namespace Nexora.Application.Abstractions.Storage;

/// <summary>Pedido de URL de upload para um ativo de marca (logo, favicon, ícone de PWA).</summary>
public sealed record BrandingUploadRequest(
    Guid TenantId,
    string Kind,
    string ContentType,
    int Bytes,
    string Sha256);

/// <summary>URL pré-assinada de upload direto (cliente → object storage, sem passar pela API) e a URL pública final.</summary>
public sealed record BrandingUpload(string UploadUrl, string PublicUrl, DateTimeOffset ExpiresAt);

/// <summary>
/// Armazenamento de mídia de marca — implementado em Infrastructure com upload pré-assinado
/// (S3-compatível). Porta de <c>branding-storage.ts</c>/<c>s3-branding-storage.ts</c> do NestJS original.
/// </summary>
public interface IBrandingStorage
{
    Task<BrandingUpload> CreateUploadAsync(BrandingUploadRequest request, CancellationToken cancellationToken = default);
}
