using Nexora.Domain.Common;
using Nexora.Domain.Finance;
using Nexora.Domain.Platform;

namespace Nexora.Domain.Provisioning;

/// <summary>Papel de sistema criado junto com o tenant (ex.: OWNER, MANAGER) — dados puros, sem I/O.</summary>
public sealed record ProvisioningRoleTemplate(string Code, string Name, IReadOnlyList<string> Permissions);

/// <summary>Praça de produção criada junto com o tenant — dados puros, sem I/O.</summary>
public sealed record ProvisioningStationTemplate(
    string Name,
    StationType Type,
    short? CapacitySlots,
    int? AvgCookSeconds,
    short SortOrder);

/// <summary>
/// Categoria de despesa/receita padrão criada junto com o tenant (Docs/Domain/12 §5) — dados
/// puros, sem I/O. <see cref="IsCmv"/> separa o que compõe o CMV do que é despesa operacional.
/// </summary>
public sealed record ProvisioningExpenseCategoryTemplate(string Name, ExpenseGroup Group, bool IsCmv);

/// <summary>
/// Conta financeira padrão criada junto com o tenant (Docs/Domain/12 §6) — dados puros, sem I/O.
/// <see cref="Type"/> é texto livre (mesma decisão de <see cref="Nexora.Domain.Finance.FinancialAccount"/>:
/// "CASH"/"BANK"/"ACQUIRER" ainda não são um enum nativo do Postgres, ver nota no domínio).
/// </summary>
public sealed record ProvisioningFinancialAccountTemplate(string Name, string Type);

/// <summary>
/// Seções de <see cref="TenantConfig"/> geradas pelo template — representadas como grafos de
/// objetos BCL (Dictionary/List) porque Domain não pode depender de um serializador; a
/// serialização para JSON acontece em Application, ao chamar <see cref="TenantConfig.CreateWithConfig"/>.
/// </summary>
public sealed record ProvisioningConfigTemplate(
    IReadOnlyDictionary<string, object?> Branding,
    IReadOnlyDictionary<string, object?> Operation,
    IReadOnlyDictionary<string, object?> Thresholds,
    IReadOnlyDictionary<string, bool> Modules,
    IReadOnlyDictionary<string, object?> Fiscal,
    IReadOnlyList<object?> Printers,
    IReadOnlyDictionary<string, object?> Payments,
    IReadOnlyDictionary<string, object?> Maintenance);

public sealed record ProvisioningTemplate(
    string Code,
    ProvisioningConfigTemplate Config,
    IReadOnlyList<ProvisioningRoleTemplate> Roles,
    IReadOnlyList<ProvisioningStationTemplate> Stations,
    IReadOnlyList<ProvisioningExpenseCategoryTemplate> ExpenseCategories,
    IReadOnlyList<ProvisioningFinancialAccountTemplate> FinancialAccounts);

/// <summary>
/// Catálogo de templates de negócio disponíveis no provisionamento de um novo tenant
/// (RF-PLT; porta de <c>packages/domain/src/provisioning/template.ts</c>). Hoje só existe
/// "PIZZERIA" — novo template é código novo aqui, nunca condicional por tenant (ADR-013):
/// o template é escolhido pelo cliente no onboarding, não amarrado a um tenant específico.
/// </summary>
public static class ProvisioningTemplates
{
    public const string Pizzeria = "PIZZERIA";

    private static readonly IReadOnlyList<string> KnownCodes = new[] { Pizzeria };

    public static bool IsKnown(string code) => KnownCodes.Contains(code, StringComparer.Ordinal);

    public static ProvisioningTemplate Get(string code)
    {
        return code switch
        {
            Pizzeria => BuildPizzeria(),
            _ => throw new DomainException($"Template de provisionamento desconhecido: {code}.")
        };
    }

    private static ProvisioningTemplate BuildPizzeria()
    {
        var config = new ProvisioningConfigTemplate(
            Branding: new Dictionary<string, object?>(),
            Operation: new Dictionary<string, object?>
            {
                ["serviceFeePercent"] = 10,
                ["serviceFeeOptional"] = true,
                ["maxDiscountPercentWithoutApproval"] = 5,
                ["halfAndHalfPricing"] = "HIGHEST",
                ["maxFractions"] = 2,
                ["stockDeductionMoment"] = "ITEM_READY",
                ["businessDayStartHour"] = 5,
                ["blockCloseWithPendingItems"] = true,
                ["blockCashCloseWithOpenTables"] = true,
                ["sessionInactivityMinutes"] = 30,
                ["bottleneck"] = new Dictionary<string, object?>
                {
                    ["resource"] = "OVEN",
                    ["slots"] = 5,
                    ["avgCookMinutes"] = 7
                }
            },
            Thresholds: new Dictionary<string, object?>
            {
                ["orderWarnMinutes"] = 12,
                ["orderCriticalMinutes"] = 18,
                ["itemInWindowMinutes"] = 2,
                ["tableIdleMinutes"] = 10,
                ["cashDivergenceAlert"] = "20.00",
                ["cmvDivergencePercent"] = 5,
                ["syncDelayMinutes"] = 5,
                ["dineInPromiseMinutes"] = 10,
                ["deliveryPromiseMinutes"] = 25
            },
            Modules: new Dictionary<string, bool>
            {
                ["dineIn"] = true,
                ["kds"] = true,
                ["cash"] = true,
                ["delivery"] = false,
                ["stock"] = false,
                ["finance"] = false
            },
            Fiscal: new Dictionary<string, object?>(),
            Printers: Array.Empty<object?>(),
            Payments: new Dictionary<string, object?>(),
            Maintenance: new Dictionary<string, object?>());

        var roles = new List<ProvisioningRoleTemplate>
        {
            new("OWNER", "Proprietário", new[] { "*" }),
            new("MANAGER", "Gerente", new[]
            {
                "order:*", "table:*", "kds:*", "cash:*", "stock:*", "report:read",
                "order:cancel_started", "cash:discount_any", "cash:close_divergent",
                "stock:adjust", "payment:refund", "order:close_with_pending",
                "user:read", "catalog:read", "catalog:write"
            }),
            new("CASHIER", "Caixa", new[]
            {
                "order:read", "order:create", "table:read", "table:close_request",
                "cash:open", "cash:close", "cash:movement", "cash:discount_limited",
                "payment:register", "report:read_own"
            }),
            new("WAITER", "Garçom", new[]
            {
                "table:open", "table:read", "table:transfer", "table:close_request",
                "order:create", "order:read", "order:add_item", "order:cancel_queued",
                "kds:read", "report:read_own"
            }),
            new("KITCHEN", "Cozinha", new[]
            {
                "kds:read", "kds:advance", "kds:refire", "catalog:set_unavailable", "order:read"
            }),
            new("STOCK", "Estoque", new[]
            {
                "stock:read", "stock:purchase", "stock:waste", "stock:count",
                "recipe:read", "recipe:write", "supplier:*"
            }),
            new("COURIER", "Entregador", new[] { "delivery:read_own", "delivery:advance" })
        };

        var stations = new List<ProvisioningStationTemplate>
        {
            new("Montagem", StationType.Assembly, null, null, 1),
            new("Forno", StationType.Oven, 5, 420, 2),
            new("Fritura", StationType.Fry, 2, 300, 3),
            new("Bar", StationType.Bar, null, 60, 4),
            new("Sobremesa", StationType.Dessert, null, 180, 5)
        };

        // Docs/Domain/12-Seeds-e-Dados-Iniciais.md §5 — passo 7 do provisionamento (RF-PLT-05).
        // is_cmv separa o que compõe o CMV do que é despesa operacional (nota do próprio doc).
        var expenseCategories = new List<ProvisioningExpenseCategoryTemplate>
        {
            new("Insumos e mercadorias", ExpenseGroup.Variable, true),
            new("Embalagens", ExpenseGroup.Variable, true),
            new("Salários", ExpenseGroup.Payroll, false),
            new("Encargos trabalhistas", ExpenseGroup.Payroll, false),
            new("Aluguel", ExpenseGroup.Fixed, false),
            new("Energia elétrica", ExpenseGroup.Fixed, false),
            new("Água", ExpenseGroup.Fixed, false),
            new("Gás", ExpenseGroup.Variable, false),
            new("Internet e telefonia", ExpenseGroup.Fixed, false),
            new("Impostos", ExpenseGroup.Tax, false),
            new("Taxas de cartão", ExpenseGroup.Variable, false),
            new("Manutenção", ExpenseGroup.Variable, false),
            new("Marketing", ExpenseGroup.Variable, false),
            new("Contabilidade", ExpenseGroup.Fixed, false),
            new("Outras despesas", ExpenseGroup.Other, false)
        };

        // Docs/Domain/12-Seeds-e-Dados-Iniciais.md §6 — passo 8 do provisionamento (RF-PLT-05).
        var financialAccounts = new List<ProvisioningFinancialAccountTemplate>
        {
            new("Caixa da loja", "CASH"),
            new("Conta bancária", "BANK"),
            new("Cielo", "ACQUIRER"),
            new("Mercado Pago", "ACQUIRER")
        };

        return new ProvisioningTemplate(Pizzeria, config, roles, stations, expenseCategories, financialAccounts);
    }
}
