using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Admin.Bugs;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Bugs.Queries.GetOperationalBugDetail;

/// <summary>
/// US-164: handler de detalhe de bug operacional, com histórico de eventos para acompanhamento.
/// </summary>
public class GetOperationalBugDetailQueryHandler(
    IOperationalBugRepository operationalBugRepository,
    IOperationalBugEventRepository operationalBugEventRepository)
    : IRequestHandler<GetOperationalBugDetailQuery, AdminBugDetailResponse>
{
    public async Task<AdminBugDetailResponse> Handle(
        GetOperationalBugDetailQuery request, CancellationToken cancellationToken)
    {
        var bug = await operationalBugRepository.GetByIdAsync(request.BugId, cancellationToken)
            ?? throw new NotFoundException(nameof(OperationalBug), request.BugId);

        var events = await operationalBugEventRepository.GetByBugIdAsync(request.BugId, cancellationToken);

        var history = events
            .Select(e => new AdminBugEventResponse(
                e.Id,
                e.EventType,
                e.OldValue,
                e.NewValue,
                e.Comment,
                e.AdminId,
                e.CreatedAtUtc))
            .ToList();

        return new AdminBugDetailResponse(
            bug.Id,
            bug.Title,
            bug.Severity,
            bug.Status,
            bug.Component,
            bug.Environment,
            bug.Origin,
            bug.Description,
            bug.CorrelationId,
            bug.RelatedTicketId,
            bug.RelatedErrorId,
            bug.AssignedAdminId,
            bug.OccurredAtUtc,
            bug.CreatedByAdminId,
            bug.CreatedAtUtc,
            history);
    }
}
