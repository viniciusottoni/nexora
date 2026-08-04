using System.Globalization;
using System.Text.Json.Nodes;

namespace Nexora.Application.Alerts.Support;

/// <summary>
/// Leitura tipada de <c>TenantConfig.Thresholds</c> (US-080 §8, JSONB livre no domínio — ver TODO em
/// <see cref="Nexora.Domain.Platform.TenantConfig"/>) — o motor de alertas é o primeiro consumidor de
/// runtime deste JSON (nenhum código lia isso antes de E-08, ver docstring de
/// <c>WaiterCallEscalationWorker</c>). Campos e valores-padrão espelham exatamente
/// Docs/Domain/01-Plataforma-e-Identidade.md §"thresholds" e o template PIZZERIA
/// (<see cref="Nexora.Domain.Provisioning.ProvisioningTemplates"/>), mais os campos que US-080
/// introduz (<see cref="AvgTimeAboveTargetPercent"/> e os de cancelamento/desconto).
/// </summary>
public sealed record AlertThresholds(
    int OrderWarnMinutes,
    int OrderCriticalMinutes,
    int ItemInWindowMinutes,
    int TableIdleMinutes,
    decimal CashDivergenceAlert,
    decimal CmvDivergencePercent,
    int SyncDelayMinutes,
    int DineInPromiseMinutes,
    int DeliveryPromiseMinutes,
    decimal AvgTimeAboveTargetPercent,
    int CancellationCountThreshold,
    int CancellationWindowMinutes,
    decimal DiscountAboveThresholdPercent,
    int DiscountWindowMinutes)
{
    /// <summary>
    /// Valores-padrão do template PIZZERIA (US-080 §4, cenário "Limiar padrão do modelo de
    /// negócio") — únicos disponíveis hoje, ver <see cref="Nexora.Domain.Provisioning.ProvisioningTemplates"/>
    /// ("hoje só existe PIZZERIA"). Tenant sem <c>thresholds</c> configurado funciona com estes
    /// valores (US-080 §10: "valores padrão sensatos, para que o estabelecimento funcione sem
    /// configurar nada").
    /// </summary>
    public static AlertThresholds Default { get; } = new(
        OrderWarnMinutes: 12,
        OrderCriticalMinutes: 18,
        ItemInWindowMinutes: 2,
        TableIdleMinutes: 10,
        CashDivergenceAlert: 20.00m,
        CmvDivergencePercent: 5m,
        SyncDelayMinutes: 5,
        DineInPromiseMinutes: 10,
        DeliveryPromiseMinutes: 25,
        AvgTimeAboveTargetPercent: 20m,
        CancellationCountThreshold: 5,
        CancellationWindowMinutes: 60,
        DiscountAboveThresholdPercent: 15m,
        DiscountWindowMinutes: 60);

    /// <summary>Faz merge raso do JSON armazenado sobre <see cref="Default"/> — chave ausente/nula mantém o padrão.</summary>
    public static AlertThresholds Parse(string? json)
    {
        var d = Default;
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return d;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return d;
        }

        if (node is not JsonObject obj)
        {
            return d;
        }

        return new AlertThresholds(
            OrderWarnMinutes: ReadInt(obj, "orderWarnMinutes", d.OrderWarnMinutes),
            OrderCriticalMinutes: ReadInt(obj, "orderCriticalMinutes", d.OrderCriticalMinutes),
            ItemInWindowMinutes: ReadInt(obj, "itemInWindowMinutes", d.ItemInWindowMinutes),
            TableIdleMinutes: ReadInt(obj, "tableIdleMinutes", d.TableIdleMinutes),
            CashDivergenceAlert: ReadDecimal(obj, "cashDivergenceAlert", d.CashDivergenceAlert),
            CmvDivergencePercent: ReadDecimal(obj, "cmvDivergencePercent", d.CmvDivergencePercent),
            SyncDelayMinutes: ReadInt(obj, "syncDelayMinutes", d.SyncDelayMinutes),
            DineInPromiseMinutes: ReadInt(obj, "dineInPromiseMinutes", d.DineInPromiseMinutes),
            DeliveryPromiseMinutes: ReadInt(obj, "deliveryPromiseMinutes", d.DeliveryPromiseMinutes),
            AvgTimeAboveTargetPercent: ReadDecimal(obj, "avgTimeAboveTargetPercent", d.AvgTimeAboveTargetPercent),
            CancellationCountThreshold: ReadInt(obj, "cancellationCountThreshold", d.CancellationCountThreshold),
            CancellationWindowMinutes: ReadInt(obj, "cancellationWindowMinutes", d.CancellationWindowMinutes),
            DiscountAboveThresholdPercent: ReadDecimal(obj, "discountAboveThresholdPercent", d.DiscountAboveThresholdPercent),
            DiscountWindowMinutes: ReadInt(obj, "discountWindowMinutes", d.DiscountWindowMinutes));
    }

    /// <summary>
    /// Aplica este conjunto de valores sobre o JSON armazenado (merge, preserva chaves
    /// desconhecidas) — usado por <c>UpdateTenantThresholdsCommandHandler</c> para um PATCH
    /// parcial (US-080 §7: o corpo do PATCH pode trazer só os campos que mudaram).
    /// </summary>
    public string ToJson()
    {
        var obj = new JsonObject
        {
            ["orderWarnMinutes"] = OrderWarnMinutes,
            ["orderCriticalMinutes"] = OrderCriticalMinutes,
            ["itemInWindowMinutes"] = ItemInWindowMinutes,
            ["tableIdleMinutes"] = TableIdleMinutes,
            ["cashDivergenceAlert"] = CashDivergenceAlert.ToString(CultureInfo.InvariantCulture),
            ["cmvDivergencePercent"] = CmvDivergencePercent,
            ["syncDelayMinutes"] = SyncDelayMinutes,
            ["dineInPromiseMinutes"] = DineInPromiseMinutes,
            ["deliveryPromiseMinutes"] = DeliveryPromiseMinutes,
            ["avgTimeAboveTargetPercent"] = AvgTimeAboveTargetPercent,
            ["cancellationCountThreshold"] = CancellationCountThreshold,
            ["cancellationWindowMinutes"] = CancellationWindowMinutes,
            ["discountAboveThresholdPercent"] = DiscountAboveThresholdPercent,
            ["discountWindowMinutes"] = DiscountWindowMinutes,
        };
        return obj.ToJsonString();
    }

    private static int ReadInt(JsonObject obj, string key, int fallback) =>
        obj.TryGetPropertyValue(key, out var value) && value is not null && value.GetValueKind() != System.Text.Json.JsonValueKind.Null
            ? value.GetValue<int>()
            : fallback;

    private static decimal ReadDecimal(JsonObject obj, string key, decimal fallback)
    {
        if (!obj.TryGetPropertyValue(key, out var value) || value is null || value.GetValueKind() == System.Text.Json.JsonValueKind.Null)
        {
            return fallback;
        }

        // ADR-017: valores monetários trafegam como string no JSON — mas alguns campos de
        // thresholds (ex.: cmvDivergencePercent, seed do template PIZZERIA) são number puro.
        // Aceita os dois formatos.
        return value.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? decimal.Parse(value.GetValue<string>(), CultureInfo.InvariantCulture)
            : value.GetValue<decimal>();
    }
}
