namespace Awaken.Contracts.Admin.Performance;

/// <summary>
/// US-220: visão agregada de performance operacional (API, banco, Redis, caches).
/// RN-002: cache sem métricas reais reporta "no_data", nunca "healthy".
/// RN-004: banco ou Redis indisponível derruba o status geral para "critical".
/// RN-005: apenas dados agregados — nenhum payload de usuário é exposto aqui.
/// </summary>
public record PerformanceOverviewResponse(
    string OverallStatus,
    DatabaseHealthResponse Database,
    RedisHealthResponse Redis,
    IReadOnlyList<CacheDomainMetricsResponse> CacheDomains,
    IReadOnlyList<RouteMetricsResponse> CriticalRoutes,
    IReadOnlyList<SlowEndpointResponse> SlowEndpoints,
    DateTime? LastCollectedAtUtc,
    string Environment);

/// <summary>Latência e status de PostgreSQL medidos via ping (CanConnectAsync + Stopwatch).</summary>
public record DatabaseHealthResponse(
    string Status,
    double? LatencyMs);

/// <summary>Latência e status de Redis medidos via IConnectionMultiplexer.PingAsync.</summary>
public record RedisHealthResponse(
    string Status,
    double? LatencyMs,
    bool IsConnected);

/// <summary>
/// Hit/miss de cache por domínio (status de acesso, catálogo de exercícios, produtos da loja).
/// Status é "no_data" sempre que não houver contador real de hits/misses disponível (RN-002).
/// </summary>
public record CacheDomainMetricsResponse(
    string Domain,
    string Status,
    long? Hits,
    long? Misses,
    double? HitRatePercent);

/// <summary>
/// p95/p99/erro/RPS por rota crítica. Status "no_data" quando não há fonte de métricas
/// agregadas e persistidas (ex.: OpenTelemetry) disponível para consulta (RN-001, RN-002).
/// </summary>
public record RouteMetricsResponse(
    string Route,
    string Status,
    double? P95Ms,
    double? P99Ms,
    double? AvgMs,
    double? ErrorRatePercent,
    double? RequestsPerSecond);

/// <summary>Endpoint identificado como lento na janela analisada. Lista pode vir vazia (sem fonte).</summary>
public record SlowEndpointResponse(
    string Route,
    double? P95Ms,
    double? ErrorRatePercent);
