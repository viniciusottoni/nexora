namespace Nexora.Domain.Platform;

/// <summary>
/// Configuração do tenant — toda a diferença de negócio entre um estabelecimento e outro
/// vive aqui (ADR-013, ADR-032), nunca em condicional de código.
/// </summary>
public sealed class TenantConfig
{
    private TenantConfig() { }

    public Guid TenantId { get; private set; }

    // TODO: value object tipado quando o formato de branding for definido — hoje é JSONB livre
    public string Branding { get; private set; } = "{}";

    // TODO: value object tipado quando o formato de operation for definido — hoje é JSONB livre
    public string Operation { get; private set; } = "{}";

    // TODO: value object tipado quando o formato de thresholds for definido — hoje é JSONB livre
    public string Thresholds { get; private set; } = "{}";

    // TODO: value object tipado quando o formato de modules for definido — hoje é JSONB livre
    public string Modules { get; private set; } = "{}";

    // TODO: value object tipado quando o formato de fiscal for definido — hoje é JSONB livre
    public string Fiscal { get; private set; } = "{}";

    // TODO: value object tipado quando o formato de printers for definido — hoje é JSONB livre
    public string Printers { get; private set; } = "[]";

    // TODO: value object tipado quando o formato de payments for definido — hoje é JSONB livre
    public string Payments { get; private set; } = "{}";

    // TODO: value object tipado quando o formato de maintenance for definido — hoje é JSONB livre
    public string Maintenance { get; private set; } = "{}";

    public int CatalogVersion { get; private set; } = 1;
    public int ConfigVersion { get; private set; } = 1;
    public int BrandingVersion { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public static TenantConfig Create(Guid tenantId)
    {
        var now = DateTimeOffset.UtcNow;

        return new TenantConfig
        {
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Cria a configuração já populada a partir de um template de provisionamento (ver
    /// <c>Nexora.Domain.Provisioning.ProvisioningTemplates</c>) — cada seção chega como JSON
    /// já serializado pela camada de Application (Domain não referencia serializador algum).
    /// </summary>
    public static TenantConfig CreateWithConfig(
        Guid tenantId,
        string brandingJson,
        string operationJson,
        string thresholdsJson,
        string modulesJson,
        string fiscalJson,
        string printersJson,
        string paymentsJson,
        string maintenanceJson)
    {
        var now = DateTimeOffset.UtcNow;

        return new TenantConfig
        {
            TenantId = tenantId,
            Branding = brandingJson,
            Operation = operationJson,
            Thresholds = thresholdsJson,
            Modules = modulesJson,
            Fiscal = fiscalJson,
            Printers = printersJson,
            Payments = paymentsJson,
            Maintenance = maintenanceJson,
            CatalogVersion = 1,
            ConfigVersion = 1,
            BrandingVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateOperation(string operationJson)
    {
        Operation = operationJson;
        ConfigVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateThresholds(string thresholdsJson)
    {
        Thresholds = thresholdsJson;
        ConfigVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateBranding(string brandingJson)
    {
        Branding = brandingJson;
        BrandingVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void BumpCatalogVersion()
    {
        CatalogVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Substitui todas as seções de configuração de uma vez, com as versões exatas informadas
    /// pela nuvem — usado pela importação do bootstrap do edge (ADR-019) e pela aplicação de
    /// cada página de sincronização inicial. Diferente de <see cref="UpdateOperation"/> e
    /// companheiros (que incrementam versão local), aqui a versão vem pronta da origem.
    /// </summary>
    public void ApplyBootstrap(
        int configVersion,
        int catalogVersion,
        string branding,
        string operation,
        string thresholds,
        string modules,
        string fiscal,
        string printers,
        string payments,
        string maintenance)
    {
        ConfigVersion = configVersion;
        CatalogVersion = catalogVersion;
        Branding = branding;
        Operation = operation;
        Thresholds = thresholds;
        Modules = modules;
        Fiscal = fiscal;
        Printers = printers;
        Payments = payments;
        Maintenance = maintenance;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
