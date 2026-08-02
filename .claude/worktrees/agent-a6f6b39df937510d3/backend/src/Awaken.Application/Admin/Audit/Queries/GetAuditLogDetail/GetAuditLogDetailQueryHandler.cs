using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Admin.Audit;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Audit.Queries.GetAuditLogDetail;

/// <summary>US-166: handler de detalhe de uma entrada de auditoria.</summary>
public class GetAuditLogDetailQueryHandler(IAuditLogRepository auditLogRepository)
    : IRequestHandler<GetAuditLogDetailQuery, AuditLogDetailResponse>
{
    public async Task<AuditLogDetailResponse> Handle(
        GetAuditLogDetailQuery request,
        CancellationToken cancellationToken)
    {
        var entry = await auditLogRepository.GetByIdAsync(request.LogId, cancellationToken)
            ?? throw new NotFoundException(nameof(AuditLog), request.LogId);

        return new AuditLogDetailResponse(
            entry.Id,
            entry.ActorUserId,
            entry.ActorType.ToString(),
            entry.Action,
            entry.ResourceType,
            entry.ResourceId,
            entry.MetadataSafe,
            entry.CorrelationId,
            entry.CreatedAtUtc);
    }
}
