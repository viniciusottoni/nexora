namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-032 (Carimbos de tempo T0 a T5) §7/§8 — deriva as sete métricas MET-001 a MET-007 a partir
/// dos seis carimbos de <see cref="Nexora.Domain.Operation.OrderItem"/>. Função pura (sem
/// <c>IApplicationDbContext</c>): consumida pelo endpoint de timeline
/// (<c>GetOrderItemTimelineQueryHandler</c>) e testável em unidade sem Testcontainers — mesmo
/// espírito de <see cref="BusinessDayPolicy"/>/<see cref="ClockSkewPolicy"/>. Nenhuma duração é
/// armazenada como número solto (ADR-034 §"Duração é calculada, nunca armazenada") — sempre
/// recalculada a partir dos carimbos.
/// </summary>
public static class OrderItemDurationCalculator
{
    /// <summary>
    /// Um intervalo é <c>null</c> quando um dos dois carimbos que o definem ainda não aconteceu —
    /// nunca vira zero nem negativo (cenário Gherkin "Item que não passa pelo gargalo":
    /// <c>cookSeconds</c>/<c>assemblySeconds</c>/<c>finishSeconds</c> ficam nulos quando o item
    /// nunca passou pelo forno, mas <c>prepSeconds</c>/<c>totalSeconds</c> continuam calculáveis
    /// direto de T1/T0 para T4/T5).
    /// </summary>
    public readonly record struct Durations(
        int? QueueSeconds,
        int? AssemblySeconds,
        int? CookSeconds,
        int? FinishSeconds,
        int? ServeSeconds,
        int? PrepSeconds,
        int? TotalSeconds);

    /// <summary>
    /// Deriva os sete intervalos (doc. 04 §4.2): T1−T0 fila (MET-001), T2−T1 montagem (MET-002),
    /// T3−T2 cocção (MET-003), T4−T3 finalização (MET-004), T5−T4 expedição (MET-005), T4−T1
    /// produção (MET-007) e T5−T0 total (MET-006).
    /// </summary>
    public static Durations Calculate(
        DateTimeOffset placedAt,
        DateTimeOffset? firedAt,
        DateTimeOffset? ovenInAt,
        DateTimeOffset? ovenOutAt,
        DateTimeOffset? readyAt,
        DateTimeOffset? servedAt)
    {
        var queueSeconds = SecondsBetween(placedAt, firedAt);
        var assemblySeconds = firedAt is { } fired ? SecondsBetween(fired, ovenInAt) : null;
        var cookSeconds = ovenInAt is { } ovenIn ? SecondsBetween(ovenIn, ovenOutAt) : null;
        var finishSeconds = ovenOutAt is { } ovenOut ? SecondsBetween(ovenOut, readyAt) : null;
        var serveSeconds = readyAt is { } ready ? SecondsBetween(ready, servedAt) : null;
        var prepSeconds = firedAt is { } firedForPrep ? SecondsBetween(firedForPrep, readyAt) : null;
        var totalSeconds = SecondsBetween(placedAt, servedAt);

        return new Durations(queueSeconds, assemblySeconds, cookSeconds, finishSeconds, serveSeconds, prepSeconds, totalSeconds);
    }

    /// <summary>
    /// <c>null</c> se <paramref name="end"/> ainda não aconteceu; caso contrário a diferença em
    /// segundos inteiros, nunca negativa (o <c>ck_item_sequence</c> do banco já garante
    /// <c>end &gt;= start</c> — este <c>Math.Max</c> é só defesa em profundidade contra
    /// arredondamento de sub-segundo).
    /// </summary>
    private static int? SecondsBetween(DateTimeOffset start, DateTimeOffset? end) =>
        end is null ? null : (int)Math.Max(0, Math.Round((end.Value - start).TotalSeconds));
}
