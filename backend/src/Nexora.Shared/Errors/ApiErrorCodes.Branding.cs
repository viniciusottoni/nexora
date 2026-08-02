namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de identidade visual (branding) por tenant (ADR-021, ADR-013).</summary>
public static partial class ApiErrorCodes
{
    /// <summary>Nenhum tenant com domínio customizado casa com o host informado.</summary>
    public const string BrandingTenantNotFound = "BRANDING_TENANT_NOT_FOUND";

    /// <summary>
    /// NOTA para quem mantém <c>ResultExtensions.MapStatusCode</c>: este código precisa de
    /// entrada explícita para 503 — é indisponibilidade de dependência externa (armazenamento
    /// de mídia sem credenciais configuradas), não erro de validação de entrada.
    /// </summary>
    public const string BrandingStorageUnavailable = "BRANDING_STORAGE_UNAVAILABLE";
}
