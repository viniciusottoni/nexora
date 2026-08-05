using System.Text.Json;

namespace Nexora.Application.Cashier.Support;

/// <summary>
/// Lê os dois limiares de caixa de <c>TenantConfig.Operation</c> (JSONB livre, ADR-032) — mesma
/// convenção de <c>Nexora.Application.Orders.Support.ServiceFeePolicy</c>/<c>PendingItemsClosePolicy</c>:
/// chave de configuração de PRODUTO (nunca condicional por tenant, ADR-013), com um default seguro
/// quando ausente/malformada.
/// </summary>
/// <remarks>
/// [DECISÃO DOCUMENTADA] Nem US-055 nem US-056 nem <c>docs/domain/04-Caixa-e-Pagamento.md</c> fixam
/// o valor exato dos dois limiares — só o contrato de chave (<c>operation.maxWithdrawalWithoutAuth</c>,
/// US-056 §8) e a existência de um "limiar configurado" para justificativa (US-055 §3.1/§4). Dois
/// limiares distintos, deliberadamente:
/// <list type="bullet">
/// <item><see cref="DefaultMaxWithdrawalWithoutAuth"/> = R$ 300,00 — valor citado no cenário Gherkin
/// "Sangria acima do limite" da própria US-056 §4 ("Dado o limite de sangria sem autorização em
/// R$ 300,00"), então não é uma escolha arbitrária desta implementação.</item>
/// <item><see cref="DefaultDivergenceJustificationThreshold"/> = R$ 5,00 — precisa ser menor que
/// R$ 6,50 (a divergência do cenário Gherkin "Divergência no fechamento" da US-055 §4, que EXIGE
/// justificativa) e maior que zero (o cenário "Fechamento sem divergência" não exige nada).
/// Deliberadamente distinto de <c>AlertThresholds.CashDivergenceAlert</c> (R$ 20,00, motor de
/// alertas E-08/US-080): aquele é o limiar em que a NUVEM varre sessões fechadas recentes e alerta
/// o gestor em lote; este é o limiar em que o PRÓPRIO fechamento, no edge, já barra a conclusão sem
/// explicação E dispara o alerta local (ver <c>CloseCashSessionCommandHandler</c>) — mesma "acima do
/// limiar" do Gherkin governando as duas consequências ao mesmo tempo, o texto da história não
/// distingue dois limiares diferentes para as duas coisas.</item>
/// </list>
/// </remarks>
public static class CashPolicy
{
    public const decimal DefaultMaxWithdrawalWithoutAuth = 300.00m;
    public const decimal DefaultDivergenceJustificationThreshold = 5.00m;

    private const string MaxWithdrawalKey = "maxWithdrawalWithoutAuth";
    private const string DivergenceThresholdKey = "cashDivergenceJustificationThreshold";

    public static decimal ResolveMaxWithdrawalWithoutAuth(string? operationJson) =>
        ReadDecimal(operationJson, MaxWithdrawalKey, DefaultMaxWithdrawalWithoutAuth);

    public static decimal ResolveDivergenceJustificationThreshold(string? operationJson) =>
        ReadDecimal(operationJson, DivergenceThresholdKey, DefaultDivergenceJustificationThreshold);

    private static decimal ReadDecimal(string? operationJson, string key, decimal fallback)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(operationJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric) && numeric >= 0)
                {
                    return numeric;
                }

                // ADR-017: dinheiro trafega como string no JSON — aceita os dois formatos, mesma
                // tolerância de AlertThresholds.ReadDecimal.
                if (value.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(value.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) &&
                    parsed >= 0)
                {
                    return parsed;
                }
            }
        }
        catch (JsonException)
        {
            // Operation malformado — cai no default seguro, mesmo espírito de ServiceFeePolicy.
        }

        return fallback;
    }
}
