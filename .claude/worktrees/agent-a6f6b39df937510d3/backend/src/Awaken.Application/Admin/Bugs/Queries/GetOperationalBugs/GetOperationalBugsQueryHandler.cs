using Awaken.Contracts.Admin.Bugs;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Bugs.Queries.GetOperationalBugs;

/// <summary>
/// US-164: handler de listagem paginada de bugs operacionais para o site admin.
/// RN-005: ordenação por CreatedAtUtc desc é garantida pelo repositório (GetPagedAsync),
/// assegurando que itens críticos recentes apareçam primeiro.
/// </summary>
public class GetOperationalBugsQueryHandler(IOperationalBugRepository operationalBugRepository)
    : IRequestHandler<GetOperationalBugsQuery, AdminBugListResponse>
{
    public async Task<AdminBugListResponse> Handle(
        GetOperationalBugsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await operationalBugRepository.GetPagedAsync(
            request.Severity,
            request.Status,
            request.Component,
            request.Environment,
            request.Origin,
            request.Page,
            request.PageSize,
            cancellationToken);

        var projected = items
            .Select(b => new AdminBugSummaryResponse(
                b.Id,
                b.Title,
                b.Severity,
                b.Status,
                b.Component,
                b.Environment,
                b.Origin,
                b.OccurredAtUtc,
                b.CreatedAtUtc))
            .ToList();

        return new AdminBugListResponse(projected, total, request.Page, request.PageSize);
    }
}
