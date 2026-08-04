using Nexora.Domain.Metrics;

namespace Nexora.Application.Alerts.Support;

/// <summary>Rótulos em pt-BR do catálogo de alertas do motor (US-080 §2) — usados na mensagem consolidada de grupo (US-083 §4: "5 pedidos atrasados").</summary>
public static class AlertMessages
{
    private static readonly IReadOnlyDictionary<string, string> PluralLabels = new Dictionary<string, string>
    {
        [AlertTypes.OrderLate] = "pedidos atrasados",
        [AlertTypes.AvgTimeAboveTarget] = "alertas de tempo médio acima da meta",
        [AlertTypes.ProductUnavailable] = "produtos indisponíveis",
        [AlertTypes.CashDivergence] = "divergências de caixa",
        [AlertTypes.SyncDelay] = "atrasos de sincronização",
        [AlertTypes.CancellationAboveThreshold] = "alertas de cancelamento acima do padrão",
        [AlertTypes.DiscountAboveThreshold] = "alertas de desconto acima do padrão",
    };

    /// <summary>US-083 §4, cenário "Rajada agrupada": mensagem direta com contagem, nunca "múltiplos alertas" (US-083 §10).</summary>
    public static string GroupMessage(string type, int count) =>
        PluralLabels.TryGetValue(type, out var label) ? $"{count} {label}" : $"{count} alertas de {type}";
}
