using System.Text.Json.Nodes;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Metrics;

namespace Nexora.Application.Alerts.Support;

/// <summary>US-082 §7 — recorte de destinatários de um tipo de alerta.</summary>
public static class AlertRoutingScopes
{
    /// <summary>Todos os usuários com o papel, no tenant inteiro.</summary>
    public const string Tenant = "TENANT";

    /// <summary>Quem responde pela entidade do alerta (ex.: garçom da mesa) — US-082 §3.1.</summary>
    public const string Responsible = "RESPONSIBLE";

    /// <summary>Garçom titular da mesa (US-082, "item pronto" — só quem está com aquela mesa).</summary>
    public const string TableOwner = "TABLE_OWNER";

    /// <summary>Quem está na praça/estação de produção do item (US-082, "praça Forno").</summary>
    public const string Station = "STATION";
}

/// <summary>Regra de direcionamento de um tipo de alerta (US-082 §7) — sempre totalmente resolvida (nunca parcial) depois de <see cref="AlertRoutingConfig.Resolve"/>.</summary>
public sealed record AlertRoutingRule(
    IReadOnlyList<string> Roles,
    string Scope,
    int? EscalateAfterSeconds,
    int? GroupWindowSeconds);

/// <summary>
/// Matriz de direcionamento por tipo de alerta (US-082) — persistida dentro de
/// <c>TenantConfig.Operation</c>, chave <c>alertRouting</c> (JSON aninhado, não uma coluna nova de
/// <c>tenant_config</c>): <c>operation</c> já é seção que flui pelo bootstrap/pull do edge (US-063),
/// então uma nova coluna top-level exigiria estender <c>TenantConfig.ApplyBootstrap</c> e os dois
/// pontos que a chamam (<c>ImportBootstrapCommandHandler</c>, <c>ProvisionTenantCommandHandler</c>)
/// — superfície grande para um risco de regressão desnecessário. Aninhar dentro de uma seção que já
/// sincroniza dá o mesmo resultado (o edge lê a matriz atualizada assim que a config chegar) sem
/// tocar nesses arquivos.
/// </summary>
public sealed record AlertRoutingConfig(IReadOnlyDictionary<string, AlertRoutingRule> Overrides)
{
    /// <summary>
    /// Matriz padrão do MVP (US-082 §7 + matriz da Visão Geral §15, restrita ao catálogo de
    /// US-080) — usada quando o tenant não personalizou o tipo (US-082 §10: "padrões sensatos...
    /// para funcionar sem configuração").
    /// </summary>
    public static IReadOnlyDictionary<string, AlertRoutingRule> Defaults { get; } = new Dictionary<string, AlertRoutingRule>
    {
        [AlertTypes.OrderLate] = new(new[] { "WAITER", "KITCHEN", "MANAGER" }, AlertRoutingScopes.Responsible, 120, 60),
        [AlertTypes.AvgTimeAboveTarget] = new(new[] { "MANAGER" }, AlertRoutingScopes.Tenant, null, 300),
        [AlertTypes.ProductUnavailable] = new(new[] { "WAITER", "CASHIER", "MANAGER" }, AlertRoutingScopes.Tenant, null, 30),
        [AlertTypes.CashDivergence] = new(new[] { "MANAGER" }, AlertRoutingScopes.Tenant, null, null),
        [AlertTypes.SyncDelay] = new(new[] { "MANAGER" }, AlertRoutingScopes.Tenant, null, null),
        [AlertTypes.CancellationAboveThreshold] = new(new[] { "MANAGER" }, AlertRoutingScopes.Tenant, null, 300),
        [AlertTypes.DiscountAboveThreshold] = new(new[] { "MANAGER" }, AlertRoutingScopes.Tenant, null, 300),
    };

    public static AlertRoutingConfig Empty { get; } = new(new Dictionary<string, AlertRoutingRule>());

    /// <summary>Regra totalmente resolvida (override do tenant, senão o padrão do tipo).</summary>
    public AlertRoutingRule Resolve(string type) =>
        Overrides.TryGetValue(type, out var rule) ? rule
        : Defaults.TryGetValue(type, out var def) ? def
        : new AlertRoutingRule(Array.Empty<string>(), AlertRoutingScopes.Tenant, null, null);

    public static AlertRoutingConfig Parse(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return Empty;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(operationJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return Empty;
        }

        if (node is not JsonObject root || root["alertRouting"] is not JsonObject routing)
        {
            return Empty;
        }

        var overrides = new Dictionary<string, AlertRoutingRule>();
        foreach (var (type, value) in routing)
        {
            if (value is not JsonObject ruleObj)
            {
                continue;
            }

            var fallback = Defaults.TryGetValue(type, out var def)
                ? def
                : new AlertRoutingRule(Array.Empty<string>(), AlertRoutingScopes.Tenant, null, null);

            var roles = ruleObj["roles"] is JsonArray rolesArray
                ? rolesArray.Select(r => r!.GetValue<string>()).ToArray()
                : fallback.Roles;

            overrides[type] = new AlertRoutingRule(
                roles,
                ruleObj["scope"]?.GetValue<string>() ?? fallback.Scope,
                ruleObj["escalateAfterSeconds"]?.GetValue<int?>() ?? fallback.EscalateAfterSeconds,
                ruleObj["groupWindowSeconds"]?.GetValue<int?>() ?? fallback.GroupWindowSeconds);
        }

        return new AlertRoutingConfig(overrides);
    }

    /// <summary>
    /// Aplica um PATCH parcial (US-082/US-083: só os campos enviados mudam, por tipo) sobre o
    /// JSON de <c>operation</c> já armazenado, devolvendo o novo JSON completo pronto para
    /// <c>TenantConfig.UpdateOperation</c>. Chaves de <c>operation</c> fora de <c>alertRouting</c>
    /// (serviceFeePercent etc.) são preservadas intactas.
    /// </summary>
    public static string ApplyPatch(string? currentOperationJson, IReadOnlyDictionary<string, AlertRoutingRulePatch> patch)
    {
        JsonObject root;
        try
        {
            root = string.IsNullOrWhiteSpace(currentOperationJson)
                ? new JsonObject()
                : (JsonNode.Parse(currentOperationJson) as JsonObject ?? new JsonObject());
        }
        catch (System.Text.Json.JsonException)
        {
            root = new JsonObject();
        }

        var routing = root["alertRouting"] as JsonObject ?? new JsonObject();
        var current = Parse(root.ToJsonString());

        foreach (var (type, p) in patch)
        {
            var effective = current.Resolve(type);
            var roles = p.Roles ?? effective.Roles;
            var scope = p.Scope ?? effective.Scope;
            var escalate = p.EscalateAfterSeconds ?? effective.EscalateAfterSeconds;
            var groupWindow = p.GroupWindowSeconds ?? effective.GroupWindowSeconds;

            var ruleObj = new JsonObject
            {
                ["roles"] = new JsonArray(roles.Select(r => JsonValue.Create(r)).ToArray()),
                ["scope"] = scope,
            };
            if (escalate is { } e) ruleObj["escalateAfterSeconds"] = e;
            if (groupWindow is { } g) ruleObj["groupWindowSeconds"] = g;

            routing[type] = ruleObj;
        }

        root["alertRouting"] = routing;
        return root.ToJsonString();
    }
}
