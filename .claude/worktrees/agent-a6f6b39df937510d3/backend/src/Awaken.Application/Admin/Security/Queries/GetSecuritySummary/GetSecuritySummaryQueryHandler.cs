using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Queries.GetSecuritySummary;

/// <summary>
/// US-219: monta o resumo preventivo da tela de Segurança.
///
/// RN-001: nenhum bloco aqui expõe dado sensível não mascarado — usuários afetados aparecem
/// apenas como Id, origens aparecem apenas como IP mascarado (MaskedIp).
/// RN-003: alertas de autenticação/token/limite/autorização que sofrem aumento repentino
/// (delta percentual vs. a janela anterior de mesmo tamanho) são marcados com IsSpike=true,
/// para destaque visual no frontend.
/// </summary>
public class GetSecuritySummaryQueryHandler(
    ISecurityAlertRepository securityAlertRepository,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetSecuritySummaryQuery, SecuritySummaryResponse>
{
    /// <summary>
    /// Tipos de alerta cujo aumento repentino deve ser destacado (RN-003):
    /// falhas de login, tokens inválidos, limite atingido e autorização negada.
    /// </summary>
    private static readonly string[] SpikeWatchedTypes =
        ["brute_force", "invalid_token", "rate_limit_hit", "rbac_denied"];

    private const double SpikeThresholdPercent = 50.0;
    private const int TopOriginsCount = 10;
    private const int TopAffectedUsersCount = 10;

    public async Task<SecuritySummaryResponse> Handle(GetSecuritySummaryQuery request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var to = request.To ?? utcNow;
        var from = request.From ?? to.AddHours(-24);

        var windowSpan = to - from;
        var previousFrom = from - windowSpan;
        var previousTo = from;

        var currentCounts = await securityAlertRepository.CountByAlertTypeAsync(from, to, cancellationToken);

        var trends = new List<AlertTypeTrendItem>();
        foreach (var current in currentCounts)
        {
            var previousCount = await securityAlertRepository.CountByAlertTypeInWindowAsync(
                current.AlertType, previousFrom, previousTo, cancellationToken);

            var deltaPercent = previousCount > 0
                ? (current.Count - previousCount) * 100.0 / previousCount
                : current.Count > 0 ? 100.0 : (double?)null;

            var isSpike = SpikeWatchedTypes.Contains(current.AlertType)
                && deltaPercent is not null
                && deltaPercent >= SpikeThresholdPercent;

            trends.Add(new AlertTypeTrendItem(current.AlertType, current.Count, previousCount, deltaPercent, isSpike));
        }

        var timeSeriesRows = await securityAlertRepository.GetHourlyTimeSeriesAsync(from, to, cancellationToken);
        var timeSeries = timeSeriesRows
            .Select(p => new SecurityTimeSeriesPoint(p.BucketUtc, p.AlertType, p.Count))
            .ToList();

        var openBySeverityRows = await securityAlertRepository.CountOpenBySeverityAsync(cancellationToken);
        var openBySeverity = openBySeverityRows
            .Select(s => new OpenAlertsBySeverityItem(s.Severity, s.Count))
            .ToList();

        var byEnvironmentRows = await securityAlertRepository.CountByEnvironmentAsync(from, to, cancellationToken);
        var byEnvironment = byEnvironmentRows
            .Select(e => new AlertsByEnvironmentItem(e.Environment, e.Count))
            .ToList();

        var topOriginsRows = await securityAlertRepository.GetTopMaskedOriginsAsync(from, to, TopOriginsCount, cancellationToken);
        var topOrigins = topOriginsRows
            .Select(o => new TopMaskedOriginItem(o.MaskedOrigin, o.Count))
            .ToList();

        var topUsersRows = await securityAlertRepository.CountByAffectedUserAsync(from, to, TopAffectedUsersCount, cancellationToken);
        var topUsers = topUsersRows
            .Select(u => new AffectedUserCountItem(u.AffectedUserId, u.Count))
            .ToList();

        var rateLimitRows = await securityAlertRepository.CountRateLimitByEndpointAsync(from, to, cancellationToken);
        var rateLimitByEndpoint = rateLimitRows
            .Select(e => new EndpointCountItem(e.Endpoint, e.Count))
            .ToList();

        var rbacDeniedRows = await securityAlertRepository.CountRbacDeniedByResourceAsync(from, to, cancellationToken);
        var rbacDeniedByResource = rbacDeniedRows
            .Select(e => new EndpointCountItem(e.Endpoint, e.Count))
            .ToList();

        var avgMinutesToAnalysis = await securityAlertRepository.GetAverageMinutesToAnalysisAsync(from, to, cancellationToken);

        return new SecuritySummaryResponse(
            from,
            to,
            trends,
            timeSeries,
            openBySeverity,
            byEnvironment,
            topOrigins,
            topUsers,
            rateLimitByEndpoint,
            rbacDeniedByResource,
            avgMinutesToAnalysis);
    }
}
