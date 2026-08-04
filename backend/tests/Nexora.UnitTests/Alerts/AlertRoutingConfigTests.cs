using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Metrics;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Alerts;

/// <summary>US-082 §4/§7 — resolução de destinatários por escopo e mescla parcial da matriz de direcionamento, sem tocar banco.</summary>
public sealed class AlertRoutingConfigTests
{
    [Fact]
    public void Resolve_Sem_Override_Usa_A_Matriz_Padrao_Do_Tipo()
    {
        var config = AlertRoutingConfig.Empty;

        var rule = config.Resolve(AlertTypes.CashDivergence);

        rule.Roles.Should().BeEquivalentTo(new[] { "MANAGER" });
        rule.Scope.Should().Be(AlertRoutingScopes.Tenant);
    }

    [Fact]
    public void Resolve_Tipo_Desconhecido_Nao_Direciona_A_Ninguem()
    {
        var rule = AlertRoutingConfig.Empty.Resolve("TIPO_INEXISTENTE");

        rule.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Le_Override_Aninhado_Dentro_De_Operation_Sem_Afetar_Outras_Chaves()
    {
        var operationJson = """
            {"serviceFeePercent": 10, "alertRouting": {"ORDER_LATE": {"roles": ["MANAGER"], "scope": "TENANT"}}}
            """;

        var config = AlertRoutingConfig.Parse(operationJson);

        config.Resolve(AlertTypes.OrderLate).Roles.Should().BeEquivalentTo(new[] { "MANAGER" });
        // Tipo não mencionado no override continua no padrão.
        config.Resolve(AlertTypes.ProductUnavailable).Roles.Should().BeEquivalentTo(new[] { "WAITER", "CASHIER", "MANAGER" });
    }

    [Fact]
    public void ApplyPatch_Atualiza_So_O_Campo_Enviado_Preservando_O_Restante_Da_Regra()
    {
        var patch = new Dictionary<string, AlertRoutingRulePatch>
        {
            [AlertTypes.OrderLate] = new(Roles: null, Scope: null, EscalateAfterSeconds: null, GroupWindowSeconds: 60),
        };

        var updatedOperationJson = AlertRoutingConfig.ApplyPatch(currentOperationJson: null, patch);
        var config = AlertRoutingConfig.Parse(updatedOperationJson);
        var rule = config.Resolve(AlertTypes.OrderLate);

        rule.GroupWindowSeconds.Should().Be(60);
        // Campos não enviados no PATCH mantêm o padrão do tipo (US-082 §7 exemplo: roles/scope de ORDER_LATE).
        rule.Roles.Should().BeEquivalentTo(new[] { "WAITER", "KITCHEN", "MANAGER" });
        rule.Scope.Should().Be(AlertRoutingScopes.Responsible);
    }

    [Fact]
    public void ApplyPatch_Preserva_Chaves_De_Operation_Fora_De_AlertRouting()
    {
        var currentOperation = """{"serviceFeePercent": 10}""";
        var patch = new Dictionary<string, AlertRoutingRulePatch>
        {
            [AlertTypes.CashDivergence] = new(Roles: null, Scope: null, EscalateAfterSeconds: 300, GroupWindowSeconds: null),
        };

        var updatedOperationJson = AlertRoutingConfig.ApplyPatch(currentOperation, patch);

        updatedOperationJson.Should().Contain("serviceFeePercent");
    }

    [Fact]
    public void Tenant_Que_Alterou_O_Direcionamento_De_Um_Tipo_Segue_A_Propria_Configuracao()
    {
        var patch = new Dictionary<string, AlertRoutingRulePatch>
        {
            [AlertTypes.ProductUnavailable] = new(Roles: new[] { "MANAGER" }, Scope: AlertRoutingScopes.Tenant, EscalateAfterSeconds: null, GroupWindowSeconds: null),
        };

        var tenantCustomJson = AlertRoutingConfig.ApplyPatch(null, patch);
        var customConfig = AlertRoutingConfig.Parse(tenantCustomJson);
        var defaultConfig = AlertRoutingConfig.Empty;

        customConfig.Resolve(AlertTypes.ProductUnavailable).Roles.Should().BeEquivalentTo(new[] { "MANAGER" });
        defaultConfig.Resolve(AlertTypes.ProductUnavailable).Roles.Should().NotBeEquivalentTo(new[] { "MANAGER" });
    }
}
