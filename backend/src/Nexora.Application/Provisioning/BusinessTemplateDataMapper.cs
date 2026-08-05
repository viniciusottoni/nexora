using System.Text.Json;
using Nexora.Domain.Common;
using Nexora.Domain.Finance;
using Nexora.Domain.Platform;
using Nexora.Domain.Provisioning;

namespace Nexora.Application.Provisioning;

/// <summary>
/// Converte entre o dado persistido de <see cref="BusinessTemplate"/> (JSON em
/// <see cref="BusinessTemplate.ConfigJson"/>/<see cref="BusinessTemplate.SeedsJson"/>) e o grafo
/// tipado <see cref="ProvisioningTemplate"/> que o restante do provisionamento já sabia consumir
/// (US-142) — mesma forma que o extinto catálogo estático
/// <c>Nexora.Domain.Provisioning.ProvisioningTemplates</c> descrevia em código C#. Vive em
/// Application porque Domain não referencia serializador algum (ADR-039).
/// </summary>
public static class BusinessTemplateDataMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Lê o JSON persistido de um <see cref="BusinessTemplate"/> e monta o grafo tipado que o handler de provisionamento consome.</summary>
    public static ProvisioningTemplate ToProvisioningTemplate(BusinessTemplate template)
    {
        var configDto = JsonSerializer.Deserialize<BusinessTemplateConfigDto>(template.ConfigJson, SerializerOptions)
            ?? throw new DomainException($"Configuração inválida no modelo de negócio {template.Code}.");
        var seedsDto = JsonSerializer.Deserialize<BusinessTemplateSeedsDto>(template.SeedsJson, SerializerOptions)
            ?? throw new DomainException($"Seeds inválidos no modelo de negócio {template.Code}.");

        var config = new ProvisioningConfigTemplate(
            Branding: configDto.Branding ?? new Dictionary<string, object?>(),
            Operation: configDto.Operation ?? new Dictionary<string, object?>(),
            Thresholds: configDto.Thresholds ?? new Dictionary<string, object?>(),
            Modules: configDto.Modules ?? new Dictionary<string, bool>(),
            Fiscal: configDto.Fiscal ?? new Dictionary<string, object?>(),
            Printers: configDto.Printers ?? new List<object?>(),
            Payments: configDto.Payments ?? new Dictionary<string, object?>(),
            Maintenance: configDto.Maintenance ?? new Dictionary<string, object?>());

        var roles = (seedsDto.Roles ?? new List<BusinessTemplateRoleDto>())
            .Select(r => new ProvisioningRoleTemplate(r.Code, r.Name, r.Permissions ?? new List<string>()))
            .ToList();

        var stations = (seedsDto.Stations ?? new List<BusinessTemplateStationDto>())
            .Select(s => new ProvisioningStationTemplate(
                s.Name,
                Enum.Parse<StationType>(s.Type, ignoreCase: true),
                s.CapacitySlots,
                s.AvgCookSeconds,
                s.SortOrder))
            .ToList();

        var expenseCategories = (seedsDto.ExpenseCategories ?? new List<BusinessTemplateExpenseCategoryDto>())
            .Select(e => new ProvisioningExpenseCategoryTemplate(
                e.Name, Enum.Parse<ExpenseGroup>(e.Group, ignoreCase: true), e.IsCmv))
            .ToList();

        var financialAccounts = (seedsDto.FinancialAccounts ?? new List<BusinessTemplateFinancialAccountDto>())
            .Select(f => new ProvisioningFinancialAccountTemplate(f.Name, f.Type))
            .ToList();

        return new ProvisioningTemplate(template.Code, config, roles, stations, expenseCategories, financialAccounts);
    }

    /// <summary>Serializa um grafo tipado (ex.: <see cref="BusinessTemplateSeedCatalog"/>) para o par de JSON que <see cref="BusinessTemplate.Create"/>/<see cref="BusinessTemplate.Update"/> persiste — usado pela migration de seed e pelos testes de round-trip.</summary>
    public static (string ConfigJson, string SeedsJson) Serialize(ProvisioningTemplate template)
    {
        var configDto = new BusinessTemplateConfigDto(
            new Dictionary<string, object?>(template.Config.Branding),
            new Dictionary<string, object?>(template.Config.Operation),
            new Dictionary<string, object?>(template.Config.Thresholds),
            new Dictionary<string, bool>(template.Config.Modules),
            new Dictionary<string, object?>(template.Config.Fiscal),
            template.Config.Printers.ToList(),
            new Dictionary<string, object?>(template.Config.Payments),
            new Dictionary<string, object?>(template.Config.Maintenance));

        var seedsDto = new BusinessTemplateSeedsDto(
            template.Roles.Select(r => new BusinessTemplateRoleDto(r.Code, r.Name, r.Permissions.ToList())).ToList(),
            template.Stations.Select(s => new BusinessTemplateStationDto(
                s.Name, s.Type.ToString(), s.CapacitySlots, s.AvgCookSeconds, s.SortOrder)).ToList(),
            template.ExpenseCategories.Select(e => new BusinessTemplateExpenseCategoryDto(
                e.Name, e.Group.ToString(), e.IsCmv)).ToList(),
            template.FinancialAccounts.Select(f => new BusinessTemplateFinancialAccountDto(f.Name, f.Type)).ToList());

        return (
            JsonSerializer.Serialize(configDto, SerializerOptions),
            JsonSerializer.Serialize(seedsDto, SerializerOptions));
    }

    /// <summary>
    /// Extrai <c>operation.bottleneck.resource</c> de forma agnóstica à origem do valor —
    /// <see cref="JsonElement"/> (caminho real, após deserialização do JSON persistido) ou
    /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> (grafo montado diretamente em código, ex.:
    /// testes/<see cref="BusinessTemplateSeedCatalog"/>). Substitui o antigo hardcode
    /// <c>if (stationTemplate.Type == StationType.Oven)</c> de
    /// <c>ProvisionTenantCommandHandler</c> — cada modelo de negócio decide sua própria praça-
    /// gargalo pela CONFIGURAÇÃO, nunca por um tipo de praça fixo em código (ADR-013): sem esta
    /// leitura genérica, hamburgueria/restaurante/lanchonete nunca teriam sua praça marcada como
    /// gargalo, porque nenhum deles usa <c>OVEN</c>.
    /// </summary>
    public static string? ExtractBottleneckResource(IReadOnlyDictionary<string, object?> operation)
    {
        if (!operation.TryGetValue("bottleneck", out var raw) || raw is null)
        {
            return null;
        }

        if (raw is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Object &&
                   element.TryGetProperty("resource", out var resourceElement) &&
                   resourceElement.ValueKind == JsonValueKind.String
                ? resourceElement.GetString()
                : null;
        }

        if (raw is IReadOnlyDictionary<string, object?> dict && dict.TryGetValue("resource", out var resourceValue))
        {
            return resourceValue switch
            {
                JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
                string s => s,
                _ => resourceValue?.ToString(),
            };
        }

        return null;
    }
}
