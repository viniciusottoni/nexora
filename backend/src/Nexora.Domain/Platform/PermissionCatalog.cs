namespace Nexora.Domain.Platform;

/// <summary>Uma entrada legível do catálogo de permissões, usada para montar telas de gestão de papéis.</summary>
public sealed record PermissionCatalogEntry(string Code, string Resource, string Description, bool Sensitive);

/// <summary>
/// Catálogo fechado de códigos de permissão do produto (ADR-023) — porta de
/// <c>packages/contracts/src/roles.ts</c> (<c>permissionCodes</c>). Fechado de propósito: papel
/// customizado escolhe um subconjunto destes códigos, nunca inventa um novo (isso seria código
/// por tenant, proibido pelo ADR-013).
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<string> AllCodes = new[]
    {
        "*",
        "table:*", "table:open", "table:read", "table:transfer", "table:close_request", "table:close", "table:manage",
        "order:*", "order:create", "order:read", "order:add_item", "order:cancel_queued",
        "order:cancel_started", "order:override_price", "order:close_with_pending",
        "kds:*", "kds:read", "kds:advance", "kds:refire",
        "cash:*", "cash:open", "cash:close", "cash:movement", "cash:discount_limited",
        "cash:discount_any", "cash:close_divergent",
        "payment:*", "payment:register", "payment:refund",
        "stock:*", "stock:read", "stock:purchase", "stock:waste", "stock:count", "stock:adjust",
        "supplier:*",
        "recipe:*", "recipe:read", "recipe:write",
        "catalog:*", "catalog:read", "catalog:write", "catalog:set_unavailable",
        "report:*", "report:read", "report:read_own",
        "finance:*", "finance:read", "finance:write",
        "delivery:read_own", "delivery:advance",
        "user:*", "user:read", "user:write",
        "config:*", "config:read", "config:write",
        "device:*", "device:manage",
        "tenant:*", "tenant:manage",
        "audit:*", "audit:read"
    };

    private static readonly Dictionary<string, string> ResourceNames = new Dictionary<string, string>
    {
        ["table"] = "Mesas",
        ["order"] = "Pedidos",
        ["kds"] = "Cozinha",
        ["cash"] = "Caixa",
        ["payment"] = "Pagamentos",
        ["stock"] = "Estoque",
        ["supplier"] = "Fornecedores",
        ["recipe"] = "Fichas técnicas",
        ["catalog"] = "Cardápio",
        ["report"] = "Relatórios",
        ["finance"] = "Financeiro",
        ["delivery"] = "Entregas",
        ["user"] = "Equipe e acessos",
        ["config"] = "Configurações",
        ["device"] = "Dispositivos",
        ["tenant"] = "Estabelecimento",
        ["audit"] = "Auditoria"
    };

    private static readonly Dictionary<string, string> ActionDescriptions = new Dictionary<string, string>
    {
        ["*"] = "Acesso completo",
        ["open"] = "Abrir",
        ["read"] = "Consultar",
        ["transfer"] = "Transferir",
        ["close_request"] = "Solicitar fechamento",
        ["close"] = "Fechar",
        ["create"] = "Criar",
        ["add_item"] = "Adicionar item",
        ["cancel_queued"] = "Cancelar item ainda na fila",
        ["cancel_started"] = "Cancelar item que já entrou em produção",
        ["override_price"] = "Alterar preço de pedido aberto",
        ["close_with_pending"] = "Fechar conta com item pendente",
        ["advance"] = "Avancar etapa",
        ["refire"] = "Solicitar refação",
        ["movement"] = "Registrar movimentação",
        ["discount_limited"] = "Aplicar desconto dentro do limite",
        ["discount_any"] = "Aplicar desconto sem limite",
        ["close_divergent"] = "Fechar caixa com divergência",
        ["register"] = "Registrar",
        ["refund"] = "Estornar pagamento",
        ["purchase"] = "Registrar compra",
        ["waste"] = "Registrar perda",
        ["count"] = "Realizar contagem",
        ["adjust"] = "Ajustar manualmente",
        ["write"] = "Alterar",
        ["set_unavailable"] = "Marcar item indisponivel",
        ["read_own"] = "Consultar somente dados proprios",
        ["manage"] = "Gerenciar"
    };

    private static readonly HashSet<string> Sensitive = new HashSet<string>
    {
        "order:cancel_started",
        "cash:discount_any",
        "cash:close_divergent",
        "stock:adjust",
        "payment:refund",
        "order:close_with_pending",
        "order:override_price",
        "audit:read"
    };

    public static IReadOnlyList<PermissionCatalogEntry> Build(IReadOnlyList<string> codes)
    {
        var entries = new List<PermissionCatalogEntry>(codes.Count);
        foreach (var code in codes)
        {
            if (code == "*")
            {
                entries.Add(new PermissionCatalogEntry(code, "Produto inteiro", "Acesso completo ao estabelecimento", true));
                continue;
            }

            var parts = code.Split(':', 2);
            var resource = parts.Length > 0 ? parts[0] : string.Empty;
            var action = parts.Length > 1 ? parts[1] : string.Empty;
            var resourceName = ResourceNames.TryGetValue(resource, out var name) ? name : resource;
            var description = action == "*"
                ? $"Acesso completo a {resourceName.ToLowerInvariant()}"
                : (ActionDescriptions.TryGetValue(action, out var actionDescription) ? actionDescription : action.Replace('_', ' '));

            entries.Add(new PermissionCatalogEntry(
                code,
                resourceName,
                description,
                Sensitive.Contains(code) || action == "*"));
        }

        return entries;
    }
}
