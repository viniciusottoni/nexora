namespace Nexora.Domain.Metrics;

// Enum nativo do PostgreSQL (documento 00, §3), mapeado como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Severidade de um alerta operacional gerado a partir de métricas ou limiares.</summary>
public enum AlertSeverity
{
    Info,
    Warning,
    High,
    Critical
}
