using Awaken.Contracts.Admin.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEvents;

public class GetSubscriptionEventsQueryHandler(
    IAdminSubscriptionDiagnosticsRepository repository)
    : IRequestHandler<GetSubscriptionEventsQuery, SubscriptionEventListResponse>
{
    public async Task<SubscriptionEventListResponse> Handle(
        GetSubscriptionEventsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPagedEventsAsync(
            request.Type,
            request.Store,
            request.Status,
            request.Plan,
            request.Product,
            request.Environment,
            request.UserId,
            request.FromUtc,
            request.ToUtc,
            request.PendingThresholdMinutes,
            request.Page,
            request.PageSize,
            cancellationToken);

        var projected = items.Select(ToSummary).ToList();

        return new SubscriptionEventListResponse(projected, total, request.Page, request.PageSize);
    }

    internal static SubscriptionEventSummaryResponse ToSummary(SubscriptionDiagnosticEventRow row) =>
        new(
            row.Id,
            row.Source,
            row.Type,
            row.Store,
            row.Status,
            row.Plan,
            row.Product,
            row.Environment,
            row.UserId,
            row.MaskedExternalRef,
            row.IsRepeatedTransaction,
            row.IsPendingTooLong,
            row.CreatedAtUtc.ToString("O"));
}
