using System.Text.Json;

namespace Nexora.Application.Catalog.FractionPricing;

/// <summary>
/// Resolve a <see cref="FractionPriceRule"/> vigente do tenant a partir do JSONB livre de
/// <c>TenantConfig.Operation</c> (chave <c>halfAndHalfPricing</c>, US-013 §8/§5 — RN-009). Função
/// pura (recebe a string JSON já carregada, não consulta banco), coberta isoladamente por
/// <c>Nexora.UnitTests.Catalog.FractionPriceRuleResolverTests</c>.
/// </summary>
public static class FractionPriceRuleResolver
{
    /// <summary>Padrão sugerido por RN-009 (hipótese não validada) quando o tenant não configurou nada, configurou um valor vazio, ou o JSON é inválido/incompleto.</summary>
    public const FractionPriceRule DefaultRule = FractionPriceRule.Highest;

    private const string PropertyName = "halfAndHalfPricing";

    public static FractionPriceRule Resolve(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return DefaultRule;
        }

        try
        {
            using var document = JsonDocument.Parse(operationJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return DefaultRule;
            }

            if (!document.RootElement.TryGetProperty(PropertyName, out var element) || element.ValueKind != JsonValueKind.String)
            {
                return DefaultRule;
            }

            var raw = element.GetString();
            return Enum.TryParse<FractionPriceRule>(raw, ignoreCase: true, out var rule) ? rule : DefaultRule;
        }
        catch (JsonException)
        {
            // operation.json malformado não deve derrubar o preview — cai no padrão de RN-009.
            return DefaultRule;
        }
    }
}
