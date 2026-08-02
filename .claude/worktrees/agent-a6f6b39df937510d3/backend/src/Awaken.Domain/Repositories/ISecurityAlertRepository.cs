using Awaken.Domain.Entities.Security;

namespace Awaken.Domain.Repositories;

public record AlertTypeCount(string AlertType, int Count);
public record AlertSeverityCount(string Severity, int Count);
public record AlertEnvironmentCount(string Environment, int Count);
public record AlertStatusCount(string Status, int Count);
public record AlertTimeSeriesPoint(DateTime BucketUtc, string AlertType, int Count);
public record AlertOriginCount(string MaskedOrigin, int Count);
public record AlertAffectedUserCount(Guid AffectedUserId, int Count);
public record AlertEndpointCount(string Endpoint, int Count);

public interface ISecurityAlertRepository
{
    Task AddAsync(SecurityAlert alert, CancellationToken ct = default);
    Task<SecurityAlert?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// US-219 RN-002: ordenação padrão prioriza Severity (critical primeiro), depois CreatedAtUtc desc.
    /// </summary>
    Task<(IReadOnlyList<SecurityAlert> Items, int Total)> GetPagedAsync(string? alertType, string? severity, string? status, string? environment, int page, int pageSize, CancellationToken ct = default);

    /// <summary>US-219: contagem de alertas por AlertType dentro de uma janela de tempo.</summary>
    Task<IReadOnlyList<AlertTypeCount>> CountByAlertTypeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>US-219: contagem de alertas abertos por severidade (indicador mínimo seção 7).</summary>
    Task<IReadOnlyList<AlertSeverityCount>> CountOpenBySeverityAsync(CancellationToken ct = default);

    /// <summary>US-219: contagem de alertas por ambiente dentro de uma janela de tempo.</summary>
    Task<IReadOnlyList<AlertEnvironmentCount>> CountByEnvironmentAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>US-219: série temporal (por hora) de alertas agrupados por tipo, para o gráfico de tendência.</summary>
    Task<IReadOnlyList<AlertTimeSeriesPoint>> GetHourlyTimeSeriesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>US-219: top origens mascaradas dentro de uma janela de tempo (RN-001: nunca origem crua).</summary>
    Task<IReadOnlyList<AlertOriginCount>> GetTopMaskedOriginsAsync(DateTime fromUtc, DateTime toUtc, int top, CancellationToken ct = default);

    /// <summary>US-219: contagem de alertas por usuário afetado dentro de uma janela de tempo.</summary>
    Task<IReadOnlyList<AlertAffectedUserCount>> CountByAffectedUserAsync(DateTime fromUtc, DateTime toUtc, int top, CancellationToken ct = default);

    /// <summary>US-219: contagem de alertas de rate_limit_hit por endpoint (Origin é usado como proxy de endpoint).</summary>
    Task<IReadOnlyList<AlertEndpointCount>> CountRateLimitByEndpointAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>US-219: contagem de alertas rbac_denied (autorização negada) por "recurso" (Origin é usado como proxy de recurso).</summary>
    Task<IReadOnlyList<AlertEndpointCount>> CountRbacDeniedByResourceAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>US-219: contagem total de alertas por AlertType dentro de uma janela (usado para cálculo de delta vs. janela anterior).</summary>
    Task<int> CountByAlertTypeInWindowAsync(string alertType, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>US-219: tempo médio (em minutos) entre criação e análise dos alertas já analisados na janela.</summary>
    Task<double?> GetAverageMinutesToAnalysisAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>
    /// US-228: verifica se já existe um alerta aberto ("open") do mesmo AlertType para o mesmo
    /// AffectedUserId, criado dentro da janela [sinceUtc, agora], evitando que o job de
    /// reconciliação crie alertas duplicados a cada execução para a mesma divergência.
    /// Quando affectedUserId é null, a checagem é feita por AlertType apenas (ex.: alertas
    /// sem usuário afetado específico).
    /// </summary>
    Task<bool> HasOpenRecentAlertAsync(string alertType, Guid? affectedUserId, DateTime sinceUtc, CancellationToken ct = default);
}
