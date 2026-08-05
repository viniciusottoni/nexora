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

    /// <summary>Código do modelo de negócio aplicado na criação (US-142) — nulo para tenants provisionados antes desta história.</summary>
    public string? TemplateCode { get; private set; }

    /// <summary>Versão do <c>business_template</c> aplicada — tenants existentes não acompanham atualizações posteriores do modelo (US-142 §4).</summary>
    public int? TemplateVersion { get; private set; }

    /// <summary>
    /// US-154 · Gestão de planos e configuração comercial — capacidades EFETIVAS do tenant,
    /// espelhadas do <c>platform_plan.CapabilitiesJson</c> corrente no momento da última
    /// reconciliação (<see cref="ApplyPlanCapabilities"/>). JSON de array de strings, mesmo padrão
    /// de JSONB livre já usado pelas demais seções desta entidade — nulo/vazio (<c>"[]"</c>) para
    /// tenants nunca reconciliados (provisionados antes desta história, ou cuja reconciliação
    /// ainda não rodou), o que <c>GetTenantPlanQueryHandler</c> trata como divergência a sanar, não
    /// como corrigido automaticamente (US-154 §10 "sem correção automática silenciosa").
    /// </summary>
    public string PlanCapabilitiesJson { get; private set; } = "[]";

    /// <summary>Versão do <see cref="PlatformPlan"/> aplicada na última reconciliação — usada para detectar divergência quando o catálogo muda depois (<see cref="PlatformPlan.Update"/> incrementa a versão do plano).</summary>
    public int? AppliedPlanVersion { get; private set; }

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
        string maintenanceJson,
        string? templateCode = null,
        int? templateVersion = null,
        string? planCapabilitiesJson = null,
        int? appliedPlanVersion = null)
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
            TemplateCode = templateCode,
            TemplateVersion = templateVersion,
            // US-154: capacidades efetivas já nascem reconciliadas com o plano confirmado no
            // provisionamento — nenhum tenant recém-criado começa divergente (ver docstring de
            // PlanCapabilitiesJson). Não usa ApplyPlanCapabilities aqui de propósito: esse método
            // incrementa ConfigVersion, e o evento tenant.config_updated já emitido pelo
            // provisionamento assume ConfigVersion=1 no payload.
            PlanCapabilitiesJson = string.IsNullOrWhiteSpace(planCapabilitiesJson) ? "[]" : planCapabilitiesJson,
            AppliedPlanVersion = appliedPlanVersion,
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

    /// <summary>US-058 (Registrar pagamento de maquininha externa) — taxas por provedor/forma (<c>{ "providers": [...] }</c>), lidas por <c>PaymentProviderFeePolicy</c>.</summary>
    public void UpdatePayments(string paymentsJson)
    {
        Payments = paymentsJson;
        ConfigVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void BumpCatalogVersion()
    {
        CatalogVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-154 · Gestão de planos e configuração comercial — reconcilia as capacidades EFETIVAS com
    /// o plano comercial corrente (catálogo <see cref="PlatformPlan"/>). Chamada explicitamente
    /// (nunca automática/silenciosa, US-154 §10): no provisionamento (plano recém-escolhido), na
    /// efetivação de uma mudança de plano, e pelo comando dedicado de reconciliação quando uma
    /// divergência é detectada e o administrador confirma a correção. Emite
    /// <c>tenant.config_updated</c>/EVT-054 (payload <c>source: "PLAN"</c>) — responsabilidade do
    /// CHAMADOR (Domain não referencia serializador/evento), mesmo padrão de
    /// <see cref="UpdateOperation"/> e companheiros.
    /// </summary>
    public void ApplyPlanCapabilities(string capabilitiesJson, int planVersion)
    {
        PlanCapabilitiesJson = string.IsNullOrWhiteSpace(capabilitiesJson) ? "[]" : capabilitiesJson;
        AppliedPlanVersion = planVersion;
        ConfigVersion++;
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
