using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Dashboard;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Admin.Dashboard.Queries.GetAdminDashboard;

/// <summary>
/// US-161 — dashboard operacional administrativo.
///
/// RN-003: nenhum dado pessoal é exposto nos blocos agregados.
/// RN-004: período padrão é últimos 7 dias quando não informado.
/// RN-005: cada bloco é resolvido em try/catch independente — a falha de um bloco
/// não derruba a resposta inteira; nesse caso o bloco assume um valor vazio/padrão
/// e a falha é registrada via ILogger (sem expor o detalhe ao cliente).
/// </summary>
public class GetAdminDashboardQueryHandler(
    IAdminAnalyticsRepository repository,
    IDateTimeService dateTimeService,
    ILogger<GetAdminDashboardQueryHandler> logger)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    public async Task<AdminDashboardResponse> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var from = request.From ?? utcNow.Date.AddDays(-7);
        var to = request.To ?? utcNow;

        var totalUsers = await SafeRunAsync(
            () => repository.CountUsersAsync(cancellationToken), 0, "TotalUsers");

        var dau = await SafeRunAsync(
            () => repository.CountDistinctActiveUsersSinceAsync(utcNow.Date, cancellationToken), 0, "Dau");

        var openTickets = await SafeRunAsync(
            () => repository.CountOpenSupportTicketsAsync(cancellationToken), 0, "OpenTickets");

        // RN-002 (US-161): MRR sem fonte confiável neste momento — retorna null,
        // o frontend deve exibir "indisponível" em vez de um valor fabricado.
        decimal? mrr = null;

        var dauTimeSeries = await SafeRunAsync(
            async () =>
            {
                var rows = await repository.GetDailyActiveUsersAsync(from, to, cancellationToken);
                return (IReadOnlyList<DauPoint>)rows.Select(r => new DauPoint(r.Date, r.Count)).ToList();
            },
            (IReadOnlyList<DauPoint>)[],
            "DauTimeSeries");

        var recentActivity = await SafeRunAsync(
            async () =>
            {
                var rows = await repository.GetRecentActivityAsync(20, cancellationToken);
                return (IReadOnlyList<ActivityFeedItem>)rows
                    .Select(r => new ActivityFeedItem(r.Action, r.ResourceType, r.CreatedAtUtc))
                    .ToList();
            },
            (IReadOnlyList<ActivityFeedItem>)[],
            "RecentActivity");

        var topEvents = await SafeRunAsync(
            async () =>
            {
                var rows = await repository.GetTopEventsAsync(from, to, 10, cancellationToken);
                return (IReadOnlyList<TopEventItem>)rows
                    .Select(r => new TopEventItem(r.Action, r.Count))
                    .ToList();
            },
            (IReadOnlyList<TopEventItem>)[],
            "TopEvents");

        return new AdminDashboardResponse(
            totalUsers,
            dau,
            openTickets,
            mrr,
            dauTimeSeries,
            recentActivity,
            topEvents);
    }

    private async Task<T> SafeRunAsync<T>(Func<Task<T>> block, T fallback, string blockName)
    {
        try
        {
            return await block();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao calcular o bloco {BlockName} do dashboard administrativo (US-161 RN-005)", blockName);
            return fallback;
        }
    }
}
