using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Ativo de mídia (imagem de produto, logo etc.), com variantes derivadas por hash de conteúdo.</summary>
public sealed class MediaAsset
{
    private MediaAsset() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string OwnerType { get; private set; } = string.Empty;
    public Guid? OwnerId { get; private set; }
    public string Variant { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public int? Bytes { get; private set; }
    public string? MimeType { get; private set; }
    public string? BlurData { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static MediaAsset Create(
        Guid tenantId,
        string ownerType,
        string variant,
        string url,
        string contentHash,
        Guid? ownerId = null,
        int? width = null,
        int? height = null,
        int? bytes = null,
        string? mimeType = null,
        string? blurData = null)
    {
        if (string.IsNullOrWhiteSpace(ownerType))
            throw new DomainException("O tipo do dono do ativo de mídia é obrigatório.");

        if (string.IsNullOrWhiteSpace(variant))
            throw new DomainException("A variante do ativo de mídia é obrigatória.");

        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("A URL do ativo de mídia é obrigatória.");

        if (string.IsNullOrWhiteSpace(contentHash))
            throw new DomainException("O hash de conteúdo do ativo de mídia é obrigatório.");

        return new MediaAsset
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Variant = variant,
            Url = url,
            ContentHash = contentHash,
            Width = width,
            Height = height,
            Bytes = bytes,
            MimeType = mimeType,
            BlurData = blurData,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
