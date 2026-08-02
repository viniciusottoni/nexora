namespace Awaken.Contracts.Admin.Security;

/// <summary>US-219: contagem de alertas por tipo na janela consultada, com delta % vs. a janela anterior equivalente.</summary>
public record AlertTypeTrendItem(
    string AlertType,
    int Count,
    int PreviousCount,
    double? DeltaPercent,
    bool IsSpike);

/// <summary>US-219: ponto da série temporal (granularidade horária) por tipo de alerta.</summary>
public record SecurityTimeSeriesPoint(DateTime BucketUtc, string AlertType, int Count);

/// <summary>US-219: contagem de alertas abertos agrupados por severidade (indicador mínimo seção 7).</summary>
public record OpenAlertsBySeverityItem(string Severity, int Count);

/// <summary>US-219: contagem de alertas agrupados por ambiente.</summary>
public record AlertsByEnvironmentItem(string Environment, int Count);

/// <summary>US-219 RN-001: origem mascarada (IP mascarado) com maior volume de alertas — nunca expõe IP completo.</summary>
public record TopMaskedOriginItem(string MaskedOrigin, int Count);

/// <summary>US-219: usuário afetado (Id apenas — sem dado pessoal) com maior volume de alertas.</summary>
public record AffectedUserCountItem(Guid AffectedUserId, int Count);

/// <summary>US-219: contagem por endpoint/recurso (proxy: campo Origin do alerta).</summary>
public record EndpointCountItem(string Endpoint, int Count);

/// <summary>
/// US-219: resumo preventivo agregado da tela de Segurança.
/// RN-001: nenhum campo aqui carrega dado sensível não mascarado.
/// RN-003: AlertTypeTrends.IsSpike sinaliza aumento repentino para destaque visual no frontend.
/// </summary>
public record SecuritySummaryResponse(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<AlertTypeTrendItem> AlertTypeTrends,
    IReadOnlyList<SecurityTimeSeriesPoint> TimeSeries,
    IReadOnlyList<OpenAlertsBySeverityItem> OpenAlertsBySeverity,
    IReadOnlyList<AlertsByEnvironmentItem> AlertsByEnvironment,
    IReadOnlyList<TopMaskedOriginItem> TopMaskedOrigins,
    IReadOnlyList<AffectedUserCountItem> TopAffectedUsers,
    IReadOnlyList<EndpointCountItem> RateLimitHitsByEndpoint,
    IReadOnlyList<EndpointCountItem> RbacDeniedByResource,
    double? AverageMinutesToAnalysis);
