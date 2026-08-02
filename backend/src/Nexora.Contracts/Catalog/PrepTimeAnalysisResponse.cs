namespace Nexora.Contracts.Catalog;

/// <summary>
/// US-016 — retorno de <c>GET /v1/catalog/variants/{id}/prep-time-analysis</c>. Compara o tempo
/// cadastrado (<see cref="ConfiguredMinutes"/>) com o histórico real de
/// <c>Nexora.Domain.Metrics.MetricProductDaily</c> nos últimos 30 dias corridos.
/// </summary>
/// <param name="VariantId">Variação analisada.</param>
/// <param name="ConfiguredMinutes">Tempo de preparo cadastrado (<c>ProductVariant.PrepMinutes</c>).</param>
/// <param name="EffectiveWarnMinutes">
/// Limiar de atenção efetivo — o próprio da variação, ou o padrão do tenant quando ela não define um.
/// </param>
/// <param name="WarnMinutesInherited">Verdadeiro quando <see cref="EffectiveWarnMinutes"/> veio do padrão do tenant, não da variação.</param>
/// <param name="EffectiveCriticalMinutes">Limiar crítico efetivo, mesma regra de herança.</param>
/// <param name="CriticalMinutesInherited">Verdadeiro quando <see cref="EffectiveCriticalMinutes"/> veio do padrão do tenant.</param>
/// <param name="ActualAvgMinutes">
/// Média real ponderada por quantidade de itens, últimos 30 dias — nulo sem nenhuma amostra no período.
/// </param>
/// <param name="ActualP90Minutes">
/// [PENDÊNCIA] sempre nulo nesta versão — <c>metric_product_daily</c> não grava p90 por
/// variação/produto hoje (só <c>avg_prep_seconds</c>; <c>MetricHourly.P90TotalSeconds</c> é
/// agregado por loja/hora, não por variação). Campo mantido no contrato para não quebrar o
/// formato do documento da US quando essa coluna existir.
/// </param>
/// <param name="SampleSize">Soma de <c>quantity</c> dos dias com registro no período (nunca contagem de dias).</param>
/// <param name="Suggestion">
/// Sugestão de novo tempo de preparo (minutos, arredondado) — só quando <see cref="SampleSize"/>
/// atinge o mínimo definido (<c>GetVariantPrepTimeAnalysisQueryHandler.MinimumSampleSize</c>, hoje
/// 20) E a divergência entre <see cref="ActualAvgMinutes"/> e <see cref="ConfiguredMinutes"/>
/// ultrapassa 20% (ver docstring do handler para a justificativa do limiar).
/// </param>
/// <param name="Note">Mensagem em português para quando não há dado suficiente — nulo caso contrário.</param>
public sealed record PrepTimeAnalysisResponse(
    Guid VariantId,
    short ConfiguredMinutes,
    short EffectiveWarnMinutes,
    bool WarnMinutesInherited,
    short EffectiveCriticalMinutes,
    bool CriticalMinutesInherited,
    decimal? ActualAvgMinutes,
    decimal? ActualP90Minutes,
    int SampleSize,
    short? Suggestion,
    string? Note);
