using Nexora.Domain.Finance;
using Nexora.Domain.Platform;
using Nexora.Domain.Provisioning;

namespace Nexora.Application.Provisioning;

/// <summary>
/// Conteúdo dos 4 modelos de negócio semeados em <c>business_template</c> pela migration
/// <c>AddBusinessTemplateSeeds</c> (US-142) — pizzaria, hamburgueria, restaurante, lanchonete.
/// PIZZERIA é o conteúdo VERBATIM do antigo catálogo estático
/// (<see cref="ProvisioningTemplates.Get"/>); os outros três são modelos novos, calibrados a partir
/// de docs/domain/12-Seeds-e-Dados-Iniciais.md §3 ("Outros modelos de negócio") — praças, limiares e
/// categorias de fato diferentes entre si, não o mesmo número com o nome trocado (US-142 §4,
/// cenário "Aplicação do modelo"). Vive em Application (não Domain) só porque é consumida pela
/// migration via <see cref="BusinessTemplateDataMapper.Serialize"/> — o catálogo em si é dado puro
/// (records de Domain), sem I/O.
/// </summary>
public static class BusinessTemplateSeedCatalog
{
    public const string PizzeriaCode = "PIZZERIA";
    public const string HamburgueriaCode = "HAMBURGUERIA";
    public const string RestauranteCode = "RESTAURANTE";
    public const string LanchoneteCode = "LANCHONETE";

    public static IReadOnlyList<(string Code, string Name, ProvisioningTemplate Template)> All() => new[]
    {
        (PizzeriaCode, "Pizzaria", Pizzeria()),
        (HamburgueriaCode, "Hamburgueria", Hamburgueria()),
        (RestauranteCode, "Restaurante", Restaurante()),
        (LanchoneteCode, "Lanchonete", Lanchonete()),
    };

    /// <summary>Verbatim do antigo <c>ProvisioningTemplates.BuildPizzeria()</c> — nada mudou no conteúdo, só a origem (dado em vez de código).</summary>
    public static ProvisioningTemplate Pizzeria() => ProvisioningTemplates.Get(ProvisioningTemplates.Pizzeria);

    /// <summary>Chapa/grelha é o gargalo (não o forno), turno mais rápido, forte em delivery via aplicativo.</summary>
    public static ProvisioningTemplate Hamburgueria() => new(
        HamburgueriaCode,
        new ProvisioningConfigTemplate(
            Branding: new Dictionary<string, object?>(),
            Operation: new Dictionary<string, object?>
            {
                ["serviceFeePercent"] = 10,
                ["serviceFeeOptional"] = true,
                ["maxDiscountPercentWithoutApproval"] = 5,
                ["maxFractions"] = 1,
                ["stockDeductionMoment"] = "ITEM_READY",
                ["businessDayStartHour"] = 5,
                ["blockCloseWithPendingItems"] = true,
                ["blockCashCloseWithOpenTables"] = true,
                ["sessionInactivityMinutes"] = 20,
                ["bottleneck"] = new Dictionary<string, object?>
                {
                    ["resource"] = "GRILL",
                    ["slots"] = 8,
                    ["avgCookMinutes"] = 5,
                },
            },
            Thresholds: new Dictionary<string, object?>
            {
                ["orderWarnMinutes"] = 8,
                ["orderCriticalMinutes"] = 14,
                ["itemInWindowMinutes"] = 2,
                ["tableIdleMinutes"] = 8,
                ["cashDivergenceAlert"] = "15.00",
                ["cmvDivergencePercent"] = 6,
                ["syncDelayMinutes"] = 5,
                ["dineInPromiseMinutes"] = 8,
                ["deliveryPromiseMinutes"] = 30,
            },
            Modules: new Dictionary<string, bool>
            {
                ["dineIn"] = true,
                ["kds"] = true,
                ["cash"] = true,
                ["delivery"] = true,
                ["stock"] = false,
                ["finance"] = false,
            },
            Fiscal: new Dictionary<string, object?>(),
            Printers: Array.Empty<object?>(),
            Payments: new Dictionary<string, object?>(),
            Maintenance: new Dictionary<string, object?>()),
        Roles: StandardRoles(),
        Stations: new List<ProvisioningStationTemplate>
        {
            new("Montagem", StationType.Assembly, null, null, 1),
            new("Chapa", StationType.Grill, 8, 300, 2),
            new("Fritura", StationType.Fry, 4, 240, 3),
            new("Bebidas", StationType.Bar, null, 60, 4),
            new("Sobremesas", StationType.Dessert, null, 120, 5),
        },
        ExpenseCategories: StandardExpenseCategories()
            .Append(new ProvisioningExpenseCategoryTemplate("Taxas de aplicativos de delivery", ExpenseGroup.Variable, false))
            .ToList(),
        FinancialAccounts: new List<ProvisioningFinancialAccountTemplate>
        {
            new("Caixa da loja", "CASH"),
            new("Conta bancária", "BANK"),
            new("Cielo", "ACQUIRER"),
            new("iFood", "ACQUIRER"),
        });

    /// <summary>À la carte, sit-down: gargalo é a montagem (não uma única praça de cocção), atendimento mais longo, taxa de serviço obrigatória, controle de estoque/financeiro mais próximo.</summary>
    public static ProvisioningTemplate Restaurante() => new(
        RestauranteCode,
        new ProvisioningConfigTemplate(
            Branding: new Dictionary<string, object?>(),
            Operation: new Dictionary<string, object?>
            {
                ["serviceFeePercent"] = 10,
                ["serviceFeeOptional"] = false,
                ["maxDiscountPercentWithoutApproval"] = 8,
                ["maxFractions"] = 1,
                ["stockDeductionMoment"] = "ITEM_READY",
                ["businessDayStartHour"] = 4,
                ["blockCloseWithPendingItems"] = true,
                ["blockCashCloseWithOpenTables"] = true,
                ["sessionInactivityMinutes"] = 60,
                ["bottleneck"] = new Dictionary<string, object?>
                {
                    ["resource"] = "ASSEMBLY",
                    ["avgCookMinutes"] = 15,
                },
            },
            Thresholds: new Dictionary<string, object?>
            {
                ["orderWarnMinutes"] = 20,
                ["orderCriticalMinutes"] = 30,
                ["itemInWindowMinutes"] = 3,
                ["tableIdleMinutes"] = 15,
                ["cashDivergenceAlert"] = "30.00",
                ["cmvDivergencePercent"] = 4,
                ["syncDelayMinutes"] = 5,
                ["dineInPromiseMinutes"] = 20,
                ["deliveryPromiseMinutes"] = 40,
            },
            Modules: new Dictionary<string, bool>
            {
                ["dineIn"] = true,
                ["kds"] = true,
                ["cash"] = true,
                ["delivery"] = false,
                ["stock"] = true,
                ["finance"] = true,
            },
            Fiscal: new Dictionary<string, object?>(),
            Printers: Array.Empty<object?>(),
            Payments: new Dictionary<string, object?>(),
            Maintenance: new Dictionary<string, object?>()),
        Roles: StandardRoles(),
        Stations: new List<ProvisioningStationTemplate>
        {
            new("Entradas e Saladas", StationType.Assembly, null, null, 1),
            new("Cozinha Quente", StationType.Grill, 6, 600, 2),
            new("Forno", StationType.Oven, 3, 900, 3),
            new("Confeitaria", StationType.Dessert, null, 300, 4),
            new("Adega e Bar", StationType.Bar, null, 90, 5),
        },
        ExpenseCategories: StandardExpenseCategories()
            .Append(new ProvisioningExpenseCategoryTemplate("Cursos e treinamento de equipe", ExpenseGroup.Fixed, false))
            .Append(new ProvisioningExpenseCategoryTemplate("Louças e utensílios", ExpenseGroup.Variable, false))
            .ToList(),
        FinancialAccounts: new List<ProvisioningFinancialAccountTemplate>
        {
            new("Caixa da loja", "CASH"),
            new("Conta bancária", "BANK"),
            new("Cielo", "ACQUIRER"),
            new("Rede", "ACQUIRER"),
            new("Mercado Pago", "ACQUIRER"),
        });

    /// <summary>Balcão rápido, sem sobremesa dedicada, sem taxa de serviço, menor estrutura de despesas e de contas financeiras.</summary>
    public static ProvisioningTemplate Lanchonete() => new(
        LanchoneteCode,
        new ProvisioningConfigTemplate(
            Branding: new Dictionary<string, object?>(),
            Operation: new Dictionary<string, object?>
            {
                ["serviceFeePercent"] = 0,
                ["serviceFeeOptional"] = false,
                ["maxDiscountPercentWithoutApproval"] = 3,
                ["maxFractions"] = 1,
                ["stockDeductionMoment"] = "ITEM_READY",
                ["businessDayStartHour"] = 5,
                ["blockCloseWithPendingItems"] = true,
                ["blockCashCloseWithOpenTables"] = false,
                ["sessionInactivityMinutes"] = 15,
                ["bottleneck"] = new Dictionary<string, object?>
                {
                    ["resource"] = "GRILL",
                    ["slots"] = 4,
                    ["avgCookMinutes"] = 4,
                },
            },
            Thresholds: new Dictionary<string, object?>
            {
                ["orderWarnMinutes"] = 6,
                ["orderCriticalMinutes"] = 10,
                ["itemInWindowMinutes"] = 1,
                ["tableIdleMinutes"] = 6,
                ["cashDivergenceAlert"] = "10.00",
                ["cmvDivergencePercent"] = 7,
                ["syncDelayMinutes"] = 5,
                ["dineInPromiseMinutes"] = 6,
                ["deliveryPromiseMinutes"] = 20,
            },
            Modules: new Dictionary<string, bool>
            {
                ["dineIn"] = true,
                ["kds"] = true,
                ["cash"] = true,
                ["delivery"] = true,
                ["stock"] = false,
                ["finance"] = false,
            },
            Fiscal: new Dictionary<string, object?>(),
            Printers: Array.Empty<object?>(),
            Payments: new Dictionary<string, object?>(),
            Maintenance: new Dictionary<string, object?>()),
        Roles: StandardRoles(),
        Stations: new List<ProvisioningStationTemplate>
        {
            new("Balcão", StationType.Assembly, null, null, 1),
            new("Chapa", StationType.Grill, 4, 180, 2),
            new("Fritura", StationType.Fry, 3, 180, 3),
            new("Bebidas", StationType.Bar, null, 45, 4),
        },
        ExpenseCategories: StandardExpenseCategories()
            .Where(c => c.Name is not ("Manutenção" or "Marketing" or "Contabilidade"))
            .ToList(),
        FinancialAccounts: new List<ProvisioningFinancialAccountTemplate>
        {
            new("Caixa da loja", "CASH"),
            new("Conta bancária", "BANK"),
            new("Mercado Pago", "ACQUIRER"),
        });

    /// <summary>
    /// Papéis de sistema — universais ao produto (docs/domain/12 §2: "Criados em toda instalação
    /// nova"), não uma diferença de modelo de negócio. Idêntico em todos os 4 templates de
    /// propósito: a granularidade de permissão não é o eixo em que pizzaria/hamburgueria/
    /// restaurante/lanchonete divergem — praças, limiares e categorias são (US-142 §4).
    /// </summary>
    private static List<ProvisioningRoleTemplate> StandardRoles() => new()
    {
        new("OWNER", "Proprietário", new[] { "*" }),
        new("MANAGER", "Gerente", new[]
        {
            "order:*", "table:*", "kds:*", "cash:*", "stock:*", "report:read",
            "order:cancel_started", "cash:discount_any", "cash:close_divergent",
            "stock:adjust", "payment:refund", "order:close_with_pending",
            "user:read", "catalog:read", "catalog:write",
        }),
        new("CASHIER", "Caixa", new[]
        {
            "order:read", "order:create", "table:read", "table:close_request",
            "cash:open", "cash:close", "cash:movement", "cash:discount_limited",
            "payment:register", "report:read_own",
        }),
        new("WAITER", "Garçom", new[]
        {
            "table:open", "table:read", "table:transfer", "table:close_request",
            "order:create", "order:read", "order:add_item", "order:cancel_queued",
            "kds:read", "report:read_own",
        }),
        new("KITCHEN", "Cozinha", new[]
        {
            "kds:read", "kds:advance", "kds:refire", "catalog:set_unavailable", "order:read",
        }),
        new("STOCK", "Estoque", new[]
        {
            "stock:read", "stock:purchase", "stock:waste", "stock:count",
            "recipe:read", "recipe:write", "supplier:*",
        }),
        new("COURIER", "Entregador", new[] { "delivery:read_own", "delivery:advance" }),
    };

    /// <summary>Base comum (docs/domain/12 §5) — cada template adiciona/remove itens para refletir sua própria estrutura de despesa.</summary>
    private static List<ProvisioningExpenseCategoryTemplate> StandardExpenseCategories() => new()
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
        new("Outras despesas", ExpenseGroup.Other, false),
    };
}
