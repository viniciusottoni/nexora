using Awaken.Contracts.Admin.Audit;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Audit.Queries.GetAuditLogs;

/// <summary>US-166: handler de listagem/filtro do log de auditoria administrativa.</summary>
public class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IRequestHandler<GetAuditLogsQuery, AuditLogListResponse>
{
    public async Task<AuditLogListResponse> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await auditLogRepository.GetPagedAsync(
            request.ActorType,
            request.Action,
            request.ResourceType,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            cancellationToken);

        var projected = items
            .Select(a => new AuditLogSummaryResponse(
                a.Id,
                a.ActorUserId,
                a.ActorType.ToString(),
                a.Action,
                a.ResourceType,
                a.ResourceId,
                a.CreatedAtUtc,
                a.CorrelationId))
            .ToList();

        return new AuditLogListResponse(projected, total, request.Page, request.PageSize);
    }
}
