using Awaken.Contracts.Admin.Analytics;
using MediatR;

namespace Awaken.Application.Admin.Analytics.Queries.GetEngagementMetrics;

/// <summary>
/// US-169 — engajamento e retenção por coorte.
/// CohortBy: "registration" | "trial_start" | "first_quest" — para o MVP, a coorte é
/// sempre baseada em User.CreatedAtUtc independentemente do valor informado, pois ainda
/// não há fonte confiável para "trial_start" ou "first_quest" nesta consulta agregada.
/// </summary>
public record GetEngagementMetricsQuery(string CohortBy, DateTime? From, DateTime? To)
    : IRequest<EngagementMetricsResponse>;
