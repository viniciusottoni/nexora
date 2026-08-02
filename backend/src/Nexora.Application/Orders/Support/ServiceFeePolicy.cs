using System.Text.Json;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// Lê <c>serviceFeePercent</c> de <c>TenantConfig.Operation</c> (JSONB livre, ADR-032) — mesma
/// convenção de <c>Nexora.Application.Catalog.Availability.BusinessDayPolicy</c>/
/// <c>Nexora.Application.Catalog.PrepTime.TenantPrepTimeDefaults</c>: uma chave de configuração
/// de produto (nunca condicional por tenant, ADR-013) com um default seguro quando
/// ausente/malformada. US-024 §5 (RN-010, hipótese): "a taxa é exibida separada e identificada
/// como opcional" — o percentual em si é decisão de cada estabelecimento.
/// </summary>
public static class ServiceFeePolicy
{
    /// <summary>10% — mesmo valor do cenário Gherkin "Taxa de serviço como estimativa" (US-024 §4), usado só quando o tenant não configurou o próprio percentual.</summary>
    public const decimal DefaultPercent = 10m;

    private const string Key = "serviceFeePercent";

    /// <summary>Lê <c>serviceFeePercent</c> (0-100) de <c>TenantConfig.Operation</c>; cai no default seguro se ausente/inválido/malformado.</summary>
    public static decimal ResolvePercent(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return DefaultPercent;
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
            // Operation malformado — cai no default seguro, mesmo espírito de BusinessDayPolicy.
        }

        return DefaultPercent;
    }

    /// <summary>
    /// Arredondamento half-up (ADR-017) do valor da taxa sobre o subtotal — mesma regra normativa
    /// usada em toda a solution para dinheiro.
    /// </summary>
    public static decimal CalculateFee(decimal subtotal, decimal percent) =>
        Math.Round(subtotal * percent / 100m, 2, MidpointRounding.AwayFromZero);
}
