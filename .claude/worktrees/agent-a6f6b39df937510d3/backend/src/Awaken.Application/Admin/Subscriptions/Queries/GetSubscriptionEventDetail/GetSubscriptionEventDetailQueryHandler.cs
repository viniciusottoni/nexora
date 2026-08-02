using Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEvents;
using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Admin.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEventDetail;

/// <summary>
/// US-217 — RN-004: detalhe nunca expõe payload bruto, apenas hash truncado e
/// referência mascarada. RN-005: inclui outros eventos do mesmo usuário para que
/// o admin perceba tentativas repetidas / múltiplos eventos sem sair da tela.
/// </summary>
public class GetSubscriptionEventDetailQueryHandler(
    IAdminSubscriptionDiagnosticsRepository repository)
    : IRequestHandler<GetSubscriptionEventDetailQuery, SubscriptionEventDetailResponse>
{
    private const int MaxRelatedEvents = 10;

    public async Task<SubscriptionEventDetailResponse> Handle(
        GetSubscriptionEventDetailQuery request, CancellationToken cancellationToken)
    {
        var row = await repository.GetEventByIdAsync(
            request.Id, request.Source, request.PendingThresholdMinutes, cancellationToken);

        if (row is null)
            throw new NotFoundException("SubscriptionEvent", request.Id);

        IReadOnlyList<SubscriptionEventSummaryResponse> relatedEvents = [];
        if (row.UserId.HasValue)
        {
            var related = await repository.GetRelatedEventsByUserIdAsync(
                row.UserId.Value, row.Id, MaxRelatedEvents, cancellationToken);
            relatedEvents = related.Select(GetSubscriptionEventsQueryHandler.ToSummary).ToList();
        }

        return new SubscriptionEventDetailResponse(
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
            row.PayloadHashMasked,
            row.IsRepeatedTransaction,
            row.IsPendingTooLong,
            row.CreatedAtUtc.ToString("O"),
            relatedEvents);
    }
}
