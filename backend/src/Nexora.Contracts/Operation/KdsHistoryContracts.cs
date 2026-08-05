namespace Nexora.Contracts.Operation;

/// <summary>
/// Contratos da US-046 (Histórico do turno no KDS) — arquivo dedicado (em vez de
/// <c>KdsContracts.cs</c>) para não colidir com quem mantém aquele arquivo (US-031, fila ativa)
/// nesta mesma onda em paralelo.
/// </summary>
public sealed record KdsHistoryItemResponse(
    Guid OrderItemId,
    Guid OrderId,
    string OrderCode,
    string ProductName,
    string? Table,
    /// <summary>T1 (US-032) — nulo no caso residual de item servido sem ter passado por Fire (fluxo manual/legado).</summary>
    DateTimeOffset? FiredAt,
    /// <summary>T4 (US-032) — nulo pelo mesmo motivo de <see cref="FiredAt"/>.</summary>
    DateTimeOffset? ReadyAt,
    DateTimeOffset ServedAt,
    /// <summary>Segundos entre <see cref="FiredAt"/> e <see cref="ReadyAt"/> (T4−T1, MET-007 — mesma fórmula de <c>OrderItemDurationCalculator</c>); 0 quando um dos dois carimbos falta.</summary>
    int PrepSeconds,
    /// <summary>Autor de T5 (quem serviu o item) — reaproveita <see cref="OrderItemTimelineActorResponse"/> (US-032), nulo quando não identificado.</summary>
    OrderItemTimelineActorResponse? Operator);

/// <summary>
/// US-046 §7 — resumo do turno. Calculado sobre o mesmo conjunto efetivamente devolvido em
/// <see cref="GetKdsHistoryResponse.Items"/> (após <c>search</c>, quando informado): a tela sempre
/// mostra o resumo do que está listado na hora, nunca um total "escondido" diferente da lista visível.
/// </summary>
public sealed record KdsHistorySummaryResponse(int Count, int AvgPrepSeconds);

/// <summary>
/// Porta de <c>GET /v1/kds/history?shift=current&amp;stationId=...&amp;search=...</c> (US-046 §7) —
/// itens SERVIDOS da praça dentro do dia operacional corrente (ADR-018), do mais recente para o
/// mais antigo.
/// </summary>
public sealed record GetKdsHistoryResponse(IReadOnlyList<KdsHistoryItemResponse> Items, KdsHistorySummaryResponse Summary);
