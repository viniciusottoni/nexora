namespace Nexora.Application.Abstractions.Storage;

/// <summary>Pedido de URL de upload para a foto de um produto do cardápio (US-010).</summary>
public sealed record ProductMediaUploadRequest(
    Guid TenantId,
    Guid ProductId,
    string ContentType,
    int Bytes,
    string Sha256);

/// <summary>URL pré-assinada de upload direto (cliente → object storage, sem passar pela API) e a URL pública final.</summary>
public sealed record ProductMediaUpload(string UploadUrl, string PublicUrl, DateTimeOffset ExpiresAt);

/// <summary>
/// Armazenamento de mídia de produto — implementado em Infrastructure com upload pré-assinado
/// (S3-compatível). Mesma estrutura de <see cref="IBrandingStorage"/> (US-003), aplicada à foto de
/// produto do cardápio (US-010 §10: "recorte assistido de imagem no upload, com proporção fixa" —
/// o recorte acontece no cliente antes do upload, este contrato só entrega a URL pré-assinada).
/// </summary>
public interface IProductMediaStorage
{
    Task<ProductMediaUpload> CreateUploadAsync(ProductMediaUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confere se a URL confirmada corresponde à chave determinística preparada para o tenant,
    /// produto, hash e MIME informados. Impede registrar URLs externas ou de outro produto.
    /// </summary>
    bool IsExpectedPublicUrl(ProductMediaUploadRequest request, string publicUrl);
}
