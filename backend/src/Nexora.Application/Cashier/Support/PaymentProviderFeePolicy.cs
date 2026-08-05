using System.Text.Json;

namespace Nexora.Application.Cashier.Support;

/// <summary>
/// US-058 §7 — lê a taxa percentual configurada por provedor/forma em
/// <c>TenantConfig.Payments</c> (JSONB livre, ADR-032): <c>{ "providers": [ { "code": "CIELO",
/// "fees": { "CREDIT": 2.8, "DEBIT": 1.5 } } ] }</c>. Mesmo padrão de default seguro de
/// <c>Nexora.Application.Orders.Support.ServiceFeePolicy</c> — provedor/forma sem configuração cai
/// em 0% (nunca inventa custo que o tenant não configurou).
/// </summary>
public static class PaymentProviderFeePolicy
{
    /// <summary>Percentual de taxa (0-100) do <paramref name="provider"/>/<paramref name="method"/>; 0 quando ausente/malformado/sem provedor.</summary>
    public static decimal ResolveFeePercent(string? paymentsJson, string? provider, string method)
    {
        if (string.IsNullOrWhiteSpace(paymentsJson) || string.IsNullOrWhiteSpace(provider))
        {
            return 0m;
        }

        try
        {
            using var document = JsonDocument.Parse(paymentsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("providers", out var providers) ||
                providers.ValueKind != JsonValueKind.Array)
            {
                return 0m;
            }

            foreach (var entry in providers.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object ||
                    !entry.TryGetProperty("code", out var code) ||
                    !string.Equals(code.GetString(), provider, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.TryGetProperty("fees", out var fees) &&
                    fees.ValueKind == JsonValueKind.Object &&
                    fees.TryGetProperty(method, out var fee) &&
                    fee.ValueKind == JsonValueKind.Number &&
                    fee.TryGetDecimal(out var percent) &&
                    percent is >= 0 and <= 100)
                {
                    return percent;
                }
            }
        }
        catch (JsonException)
        {
            // Payments malformado — cai no default seguro (0%), mesmo espírito de ServiceFeePolicy.
        }

        return 0m;
    }

    /// <summary>Arredondamento half-up (ADR-017) da taxa sobre o valor bruto do pagamento.</summary>
    public static decimal CalculateFee(decimal amount, decimal percent) =>
        Math.Round(amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
}
