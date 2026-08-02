using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Analytics;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Analytics.Queries.GetEngagementMetrics;

/// <summary>
/// US-169 — engajamento e retenção por coorte.
///
/// RN-003: a retenção (D1/D7/D30) deve indicar "dados insuficientes" (InsufficientData=true,
/// RetentionRate=null) em vez de uma taxa zero enganosa, quando a coorte ainda não tem
/// usuários com tempo suficiente decorrido desde o registro.
///
/// NOTA (CohortBy): para o MVP, a coorte é sempre baseada em User.CreatedAtUtc — não há
/// fonte confiável de dados para basear a coorte em "trial_start" ou "first_quest" nesta
/// consulta agregada, então o valor de CohortBy é aceito mas não altera o cálculo.
/// </summary>
public class GetEngagementMetricsQueryHandler(
    IAdminAnalyticsRepository repository,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetEngagementMetricsQuery, EngagementMetricsResponse>
{
    public async Task<EngagementMetricsResponse> Handle(GetEngagementMetricsQuery request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var from = request.From ?? utcNow.Date.AddDays(-30);
        var to = request.To ?? utcNow;

        var dau = await repository.CountDistinctActiveUsersSinceAsync(utcNow.Date, cancellationToken);
        var mau = await repository.CountDistinctActiveUsersSinceAsync(utcNow.AddDays(-30), cancellationToken);
        double? dauMauRatio = mau > 0 ? (double)dau / mau : null;

        var retentionD1 = await ComputeRetentionAsync(utcNow, 1, cancellationToken);
        var retentionD7 = await ComputeRetentionAsync(utcNow, 7, cancellationToken);
        var retentionD30 = await ComputeRetentionAsync(utcNow, 30, cancellationToken);

        var featureUsageRows = await repository.GetFeatureUsageAsync(from, to, 10, cancellationToken);
        var featureUsage = featureUsageRows
            .Select(r => new FeatureUsageItem(r.Action, r.Count))
            .ToList();

        return new EngagementMetricsResponse(
            dau,
            mau,
            dauMauRatio,
            retentionD1,
            retentionD7,
            retentionD30,
            featureUsage);
    }

    /// <summary>
    /// % de usuários registrados há pelo menos <paramref name="offsetDays"/> dias que tiveram
    /// pelo menos um AuditLog (ActorType=User) criado estritamente após CreatedAtUtc + offsetDays.
    /// RN-003: se não houver usuários velhos o suficiente para a janela, retorna InsufficientData=true.
    /// </summary>
    private async Task<RetentionCohort> ComputeRetentionAsync(DateTime utcNow, int offsetDays, CancellationToken ct)
    {
        var threshold = utcNow.AddDays(-offsetDays);
        var eligibleUsers = await repository.GetUsersRegisteredBeforeAsync(threshold, ct);

        if (eligibleUsers.Count == 0)
            return new RetentionCohort(null, true);

        var userIds = eligibleUsers.Select(u => u.UserId).ToList();
        var retainedUserIds = await repository.GetUserIdsWithActivityAfterOffsetAsync(userIds, offsetDays, ct);

        var rate = (double)retainedUserIds.Count / eligibleUsers.Count;
        return new RetentionCohort(rate, false);
    }
}
