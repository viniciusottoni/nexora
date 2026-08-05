using System.Text.Json.Serialization;

namespace Nexora.Application.Provisioning;

/// <summary>
/// Forma serializável de <c>Nexora.Domain.Provisioning.ProvisioningConfigTemplate</c> — grava
/// <c>business_template.config</c> (JSONB). Fica em Application (não Domain) porque é aqui que o
/// serializador entra em cena (ADR-039: Domain não referencia nenhum). Nomes de propriedade em
/// <c>camelCase</c> (via <see cref="JsonPropertyNameAttribute"/>) para bater com a convenção já
/// usada dentro de cada seção (ex.: "serviceFeePercent") — consistência de estilo dentro do mesmo
/// documento JSON.
/// </summary>
public sealed record BusinessTemplateConfigDto(
    [property: JsonPropertyName("branding")] Dictionary<string, object?>? Branding,
    [property: JsonPropertyName("operation")] Dictionary<string, object?>? Operation,
    [property: JsonPropertyName("thresholds")] Dictionary<string, object?>? Thresholds,
    [property: JsonPropertyName("modules")] Dictionary<string, bool>? Modules,
    [property: JsonPropertyName("fiscal")] Dictionary<string, object?>? Fiscal,
    [property: JsonPropertyName("printers")] List<object?>? Printers,
    [property: JsonPropertyName("payments")] Dictionary<string, object?>? Payments,
    [property: JsonPropertyName("maintenance")] Dictionary<string, object?>? Maintenance);

/// <summary>Forma serializável de <c>ProvisioningRoleTemplate</c> — item de <c>business_template.seeds.roles</c>.</summary>
public sealed record BusinessTemplateRoleDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("permissions")] List<string>? Permissions);

/// <summary>Forma serializável de <c>ProvisioningStationTemplate</c> — item de <c>business_template.seeds.stations</c>.</summary>
public sealed record BusinessTemplateStationDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("capacitySlots")] short? CapacitySlots,
    [property: JsonPropertyName("avgCookSeconds")] int? AvgCookSeconds,
    [property: JsonPropertyName("sortOrder")] short SortOrder);

/// <summary>Forma serializável de <c>ProvisioningExpenseCategoryTemplate</c> — item de <c>business_template.seeds.expenseCategories</c>.</summary>
public sealed record BusinessTemplateExpenseCategoryDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("isCmv")] bool IsCmv);

/// <summary>Forma serializável de <c>ProvisioningFinancialAccountTemplate</c> — item de <c>business_template.seeds.financialAccounts</c>.</summary>
public sealed record BusinessTemplateFinancialAccountDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type);

/// <summary>Forma serializável de todos os seeds de um template — grava <c>business_template.seeds</c> (JSONB).</summary>
public sealed record BusinessTemplateSeedsDto(
    [property: JsonPropertyName("roles")] List<BusinessTemplateRoleDto>? Roles,
    [property: JsonPropertyName("stations")] List<BusinessTemplateStationDto>? Stations,
    [property: JsonPropertyName("expenseCategories")] List<BusinessTemplateExpenseCategoryDto>? ExpenseCategories,
    [property: JsonPropertyName("financialAccounts")] List<BusinessTemplateFinancialAccountDto>? FinancialAccounts);
