using FluentAssertions;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// E-09/US-090 §12, "Teste de cobertura falha se uma ação sensível não registrar" — registro único
/// e explícito das 7 ações do RF-AUD-02, para que a lista nunca fique só na cabeça de quem escreveu
/// a US (US-090 §15, "ação sensível esquecida cria um ponto cego permanente").
/// </summary>
/// <remarks>
/// Este teste NÃO reexecuta as 4 ações já implementadas de ponta a ponta — cada uma já tem
/// cobertura funcional dedicada que efetivamente insere e lê <c>audit_log</c> sob RLS real:
/// cancelamento (<c>CancelOrderIntegrationTests</c>), alteração de preço
/// (<c>PricingIntegrationTests</c>), alteração de permissão (<see cref="RoleAuditTests"/>) e
/// desconto/autorização elevada (<c>CancelOrderIntegrationTests</c>, fluxo que passa por
/// <c>AuthorizeSensitiveActionCommand</c>). Repetir aqui o setup de pedido/produto/sessão desses
/// testes só para "provar de novo" seria duplicação sem ganho — o valor deste teste é outro:
/// falhar em CI se o command de um desses handlers for removido/renomeado sem que ninguém
/// atualize este registro, e deixar as 3 ações SEM handler (movimentação de estoque, ajuste
/// financeiro, abertura/fechamento de caixa) explicitamente marcadas como pendência conhecida, não
/// esquecida — decisão registrada durante a implementação de E-09 (essas 3 dependem de módulos de
/// Application que ainda não existem: Cashier, Inventory, Finance só têm entidade de domínio, sem
/// caso de uso, e modelar a regra de negócio delas pertence aos épicos próprios, não a E-09).
/// </remarks>
public sealed class AuditCoverageTests
{
    /// <summary>
    /// Ação RF-AUD-02 → command que representa o caso de uso. A convenção do repositório
    /// (ver CLAUDE.md, "Commands/&lt;NomeDoCommand&gt;/") é um handler <c>{Command}Handler</c> no
    /// MESMO namespace do command — verificado abaixo via reflection, sem precisar apontar para o
    /// tipo do handler diretamente (que é <c>internal</c> em todo o codebase).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Type> ImplementedActions = new Dictionary<string, Type>
    {
        ["cancelamento"] = typeof(Application.Orders.Commands.CancelOrder.CancelOrderCommand),
        ["desconto (via autorização elevada)"] = typeof(Application.Auth.Commands.AuthorizeSensitiveAction.AuthorizeSensitiveActionCommand),
        ["alteração de preço"] = typeof(Application.Catalog.Prices.Commands.SetVariantPrice.SetVariantPriceCommand),
        ["alteração de permissão"] = typeof(Application.Roles.Commands.UpdateRole.UpdateRoleCommand),
    };

    /// <summary>
    /// Ações do RF-AUD-02 sem handler de Application ainda — decisão explícita tomada ao
    /// implementar E-09 (ver docstring da classe).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> PendingActions = new Dictionary<string, string>
    {
        ["movimentação de estoque"] = "Nexora.Domain.Inventory.StockMovement existe; nenhum comando/handler de Application ainda.",
        ["ajuste financeiro (sangria/suprimento)"] = "Nexora.Domain.Cashier.CashMovement/Finance.FinancialEntry existem; nenhum comando/handler de Application ainda.",
        ["abertura e fechamento de caixa"] = "Nexora.Domain.Cashier.CashSession existe; nenhum comando/handler de Application ainda.",
    };

    [Fact]
    public void RF_AUD_02_Lista_Sete_Acoes_Entre_Implementadas_E_Pendentes()
    {
        (ImplementedActions.Count + PendingActions.Count).Should().Be(7,
            "RF-AUD-02 define exatamente 7 ações sensíveis — qualquer alteração nesse número exige atualizar este registro conscientemente.");
    }

    [Theory]
    [MemberData(nameof(ImplementedActionNames))]
    public void Acao_Implementada_Tem_Command_E_Handler_De_Application(string actionName)
    {
        var commandType = ImplementedActions[actionName];
        var expectedHandlerName = commandType.Name + "Handler";

        var handlerExists = commandType.Assembly.GetTypes()
            .Any(t => t.Namespace == commandType.Namespace && t.Name == expectedHandlerName);

        handlerExists.Should().BeTrue(
            $"a ação '{actionName}' precisa continuar tendo {expectedHandlerName} em {commandType.Namespace} (RF-AUD-02).");
    }

    [Fact]
    public void Acoes_Pendentes_Continuam_Documentadas_Com_Motivo()
    {
        PendingActions.Should().AllSatisfy(kv => kv.Value.Should().NotBeNullOrWhiteSpace());
    }

    public static IEnumerable<object[]> ImplementedActionNames() =>
        ImplementedActions.Keys.Select(name => new object[] { name });
}
