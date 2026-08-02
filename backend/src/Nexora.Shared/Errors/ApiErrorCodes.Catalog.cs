namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de catálogo (categorias e produtos) — US-010 (ADR-021).</summary>
public static partial class ApiErrorCodes
{
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";

    /// <summary>Categoria referenciada por um produto (criação/edição) não existe ou não pertence ao tenant.</summary>
    public const string ProductCategoryNotFound = "PRODUCT_CATEGORY_NOT_FOUND";

    /// <summary>Praça referenciada por um produto (campo opcional <c>stationId</c>) não existe ou não pertence ao tenant.</summary>
    public const string ProductStationNotFound = "PRODUCT_STATION_NOT_FOUND";

    /// <summary>Reordenação recusada — o conjunto de ids enviado não bate com os itens existentes (faltando/sobrando/duplicado).</summary>
    public const string CatalogReorderSetMismatch = "CATALOG_REORDER_SET_MISMATCH";

    /// <summary>Armazenamento de mídia de produto (S3) não configurado — mesma família de <see cref="BrandingStorageUnavailable"/>.</summary>
    public const string ProductMediaStorageUnavailable = "PRODUCT_MEDIA_STORAGE_UNAVAILABLE";

    /// <summary>Ativo de mídia referenciado em <c>ConfirmProductImage</c> não corresponde a nenhum upload preparado para o produto.</summary>
    public const string ProductMediaNotFound = "PRODUCT_MEDIA_NOT_FOUND";

    /// <summary>Cardápio público (<c>GET /v1/public/menu</c>) — host sem tenant/estabelecimento correspondente.</summary>
    public const string PublicMenuTenantNotFound = "PUBLIC_MENU_TENANT_NOT_FOUND";

    // US-011 (Variações de produto com preço próprio).

    /// <summary>Variante referenciada (<c>PATCH/GET .../variants/:id</c>, <c>POST .../prices</c> etc.) não existe ou não pertence ao tenant.</summary>
    public const string VariantNotFound = "VARIANT_NOT_FOUND";

    /// <summary>Canal enviado em <c>SetVariantPrice</c>/<c>CreateVariant</c> não corresponde a um valor válido de <c>Channel</c> (DineIn/Delivery/Takeout/Marketplace).</summary>
    public const string PriceChannelInvalid = "PRICE_CHANNEL_INVALID";
}
