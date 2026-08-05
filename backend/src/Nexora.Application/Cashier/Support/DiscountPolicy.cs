using System.Text.Json;

namespace Nexora.Application.Cashier.Support;

/// <summary>
/// US-054 §7 — lê <c>operation.maxDiscountWithoutAuthPercent</c> de <c>TenantConfig.Operation</c>
/// (JSONB livre, ADR-032). Mesmo padrão de default seguro de
/// <c>Nexora.Application.Orders.Support.ServiceFeePolicy</c>.
/// </summary>
public static class DiscountPolicy
{
    /// <summary>5% — limiar conservador quando o tenant não configurou o próprio limite (US-054 §15: "calibrar no piloto").</summary>
    public const decimal DefaultMaxWithoutAuthPercent = 5m;

    private const string Key = "maxDiscountWithoutAuthPercent";

    public static decimal ResolveMaxWithoutAuthPercent(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return DefaultMaxWithoutAuthPercent;
        }

        try
        {
            using var document = JsonDocument.Parse(operationJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(Key, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetDecimal(out var percent) &&
                percent is >= 0 and <= 100)
            {
                return percent;
            }
        }
        catch (JsonException)
        {
            // Operation malformado — cai no default seguro.
        }

        return DefaultMaxWithoutAuthPercent;
    }
}
