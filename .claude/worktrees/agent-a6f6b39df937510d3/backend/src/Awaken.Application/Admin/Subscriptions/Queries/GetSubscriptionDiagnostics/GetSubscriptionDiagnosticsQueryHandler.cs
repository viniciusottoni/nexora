using Awaken.Contracts.Admin.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionDiagnostics;

public class GetSubscriptionDiagnosticsQueryHandler(
    IAdminSubscriptionDiagnosticsRepository repository)
    : IRequestHandler<GetSubscriptionDiagnosticsQuery, SubscriptionDiagnosticsResponse>
{
    public async Task<SubscriptionDiagnosticsResponse> Handle(
        GetSubscriptionDiagnosticsQuery request, CancellationToken cancellationToken)
    {
        var counts = await repository.GetCountsAsync(
            request.FromUtc, request.ToUtc, request.PendingThresholdMinutes, cancellationToken);

        return new SubscriptionDiagnosticsResponse(
            counts.ApprovedCount,
            counts.DeniedCount,
            counts.PendingCount,
            counts.FailedCount,
            counts.RepeatedTransactionsCount,
            counts.PendingGrantsCount);
    }
}
