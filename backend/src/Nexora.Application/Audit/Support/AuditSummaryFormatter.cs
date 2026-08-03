using System.Globalization;
using System.Text.Json;

namespace Nexora.Application.Audit.Support;

/// <summary>
/// Traduz <c>AuditLog.Action</c> + <c>Before</c>/<c>After</c> (JSONB livre, já desserializado pelo
/// chamador via <see cref="TryParseJson"/>) para uma frase em português (US-091 §4, cenário "Antes
/// e depois legíveis" — "não deve exibir JSON bruto ao gestor"). Cobre as ações que já emitem
/// auditoria hoje (ver <c>AuditCoverageTests</c>); ação desconhecida cai num resumo genérico em vez
/// de quebrar a consulta.
/// </summary>
public static class AuditSummaryFormatter
{
    public static string Format(string action, JsonElement? before, JsonElement? after)
    {
        return action switch
        {
            "ORDER_CANCELLED" => "Pedido cancelado",
            "ORDER_ITEM_CANCELLED" => "Item do pedido cancelado",
            "ORDER_CANCEL_DENIED" or "ORDER_ITEM_CANCEL_DENIED" => "Tentativa de cancelamento recusada — autorização necessária",
            "PERMISSION_CHANGED" => "Permissões do papel alteradas",
            "ROLE_UPDATED" => "Papel atualizado",
            "VARIANT_PRICE_CHANGED" or "PRICE_CHANGED" => FormatPriceChanged(before, after),
            "PRICE_BULK_ADJUSTED" => FormatBulkPriceAdjusted(after),
            "SUPPORT_ACCESS_GRANTED" => "Acesso de suporte da plataforma concedido",
            "tenant.cross_tenant_access_attempt" => "Tentativa de acesso a estabelecimento de outro tenant",
            _ => FormatGeneric(action),
        };
    }

    /// <summary>
    /// Parse seguro e compartilhado de JSONB livre — mesmo helper usado pelo chamador para os
    /// campos <c>before</c>/<c>after</c> da resposta, para nunca desserializar a mesma string duas
    /// vezes (uma para o resumo, outra para o payload devolvido ao front).
    /// </summary>
    public static JsonElement? TryParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatPriceChanged(JsonElement? before, JsonElement? after)
    {
        // Ambos os produtores atuais (SetVariantPriceCommandHandler, SetVariantChannelPriceCommandHandler,
        // BulkAdjustPricesByCategoryCommandHandler) gravam a chave "amount" tanto em before quanto em
        // after — "newAmount"/"oldAmount" só existem no payload do DomainEvent, nunca no AuditLog.
        var newAmount = ReadDecimal(after, "amount");
        var oldAmount = ReadDecimal(before, "amount");

        if (newAmount is null)
        {
            return "Preço alterado";
        }

        return oldAmount is null
            ? $"Preço definido em {FormatMoney(newAmount.Value)}"
            : $"Preço alterado de {FormatMoney(oldAmount.Value)} para {FormatMoney(newAmount.Value)}";
    }

    private static string FormatBulkPriceAdjusted(JsonElement? after)
    {
        var percent = ReadDecimal(after, "percent");
        var updated = ReadInt(after, "updated");
        var percentText = percent is null ? "" : $" de {percent.Value.ToString("0.##%", CultureInfo.GetCultureInfo("pt-BR"))}";
        return updated is null
            ? $"Reajuste em massa de preços{percentText}"
            : $"Reajuste em massa{percentText} aplicado a {updated} item(ns)";
    }

    /// <summary>
    /// Fallback para qualquer ação sem frase dedicada acima — inclui as ações dinâmicas de
    /// <c>AuthorizeSensitiveActionCommandHandler</c> (o próprio <paramref name="action"/> JÁ É o
    /// código da ação sensível, ex. <c>CANCEL_STARTED_ITEM</c> — não há necessidade de procurar
    /// dentro do JSON de contexto, que nem carrega esse dado).
    /// </summary>
    private static string FormatGeneric(string action) =>
        $"Ação sensível: {action.Replace('_', ' ').ToLowerInvariant()}";

    private static string FormatMoney(decimal amount) =>
        amount.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    private static decimal? ReadDecimal(JsonElement? element, string property)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(property, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static int? ReadInt(JsonElement? element, string property)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(property, out var prop) ||
            prop.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return prop.GetInt32();
    }
}
