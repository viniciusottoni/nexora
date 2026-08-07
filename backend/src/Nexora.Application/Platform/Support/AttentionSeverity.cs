namespace Nexora.Application.Platform.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — severidade de um item da fila de
/// atenção (<c>GetAttentionQueueQuery</c>). Quatro níveis (não só "crítico/normal" como o ranking de
/// status do diretório, US-151 <c>TenantAttentionRanking</c>) porque o critério de aceite "Priorização
/// explicável" exige que "a ordenação priorize criticidade SEM ESCONDER itens menos graves" — ou
/// seja, a fila precisa de gradação visível, não um corte binário.
/// </summary>
public enum AttentionSeverity
{
    Critical,
    High,
    Medium,
    Low
}

public static class AttentionSeverityExtensions
{
    /// <summary>Ordem de prioridade — 0 é o mais crítico (mesma convenção de <c>TenantAttentionRanking.RankOf</c>: menor rank ordena primeiro).</summary>
    public static int RankOf(this AttentionSeverity severity) => severity switch
    {
        AttentionSeverity.Critical => 0,
        AttentionSeverity.High => 1,
        AttentionSeverity.Medium => 2,
        AttentionSeverity.Low => 3,
        _ => 4
    };

    /// <summary>Rótulo estável do contrato de API — vinculado por <c>[FromQuery(Name = "severity")]</c> como enum (igual ao filtro <c>status</c> do diretório de tenants).</summary>
    public static string ToWireLabel(this AttentionSeverity severity) => severity switch
    {
        AttentionSeverity.Critical => "CRITICAL",
        AttentionSeverity.High => "HIGH",
        AttentionSeverity.Medium => "MEDIUM",
        AttentionSeverity.Low => "LOW",
        _ => "LOW"
    };
}
