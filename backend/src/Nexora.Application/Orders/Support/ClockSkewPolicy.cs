namespace Nexora.Application.Orders.Support;

/// <summary>
/// ADR-034 (Relógio, sequência e tolerância a desvio) — resolve o <c>occurred_at</c> de uma
/// transição de <see cref="Nexora.Domain.Operation.OrderItem"/> a partir do horário informado pelo
/// dispositivo (header <c>X-Occurred-At</c>) contra o relógio do próprio edge: "diferença ≤ 2 min
/// → aceita o do cliente; diferença > 2 min → usa o do edge + registra o desvio". Função pura
/// (sem <c>IApplicationDbContext</c>/HTTP), mesma convenção de
/// <see cref="Nexora.Application.Catalog.Availability.BusinessDayPolicy"/>/<see cref="ServiceFeePolicy"/>
/// — testável em unidade, único lugar da solution que decide isto (evita cada handler reimplementar
/// a regra com um desvio sutil de comportamento).
/// </summary>
public static class ClockSkewPolicy
{
    /// <summary>Tolerância exata do ADR-034 — "diferença ≤ 2 min → aceita o do cliente".</summary>
    public static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Resultado da resolução: <see cref="OccurredAt"/> é o horário efetivamente gravado nos
    /// carimbos/no <c>DomainEvent</c>; <see cref="ClockSuspect"/> espelha
    /// <c>DomainEvent.ClockSuspect</c> (ADR-034 "registra o desvio para diagnóstico");
    /// <see cref="Deviation"/> é a diferença bruta (dispositivo − edge), só para log estruturado —
    /// positiva quando o relógio do dispositivo está adiantado.
    /// </summary>
    public readonly record struct Resolution(DateTimeOffset OccurredAt, bool ClockSuspect, TimeSpan? Deviation);

    /// <summary>
    /// <paramref name="deviceOccurredAt"/> nulo (nenhum <c>X-Occurred-At</c> enviado) sempre cai no
    /// relógio do edge, sem desvio a registrar — o caso comum de dispositivo sem esse header.
    /// </summary>
    public static Resolution Resolve(DateTimeOffset? deviceOccurredAt, DateTimeOffset edgeNowUtc)
    {
        if (deviceOccurredAt is not { } deviceAt)
        {
            return new Resolution(edgeNowUtc, ClockSuspect: false, Deviation: null);
        }

        var deviation = deviceAt - edgeNowUtc;

        return deviation.Duration() <= Tolerance
            ? new Resolution(deviceAt, ClockSuspect: false, deviation)
            : new Resolution(edgeNowUtc, ClockSuspect: true, deviation);
    }
}
