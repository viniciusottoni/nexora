using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Bugs.Commands.CreateOperationalBug;

/// <summary>
/// US-171: handler de registro manual de bug/incidente interno.
/// Auditoria não inclui a descrição completa (apenas severidade/componente/ambiente — ADR-015).
/// </summary>
public class CreateOperationalBugCommandHandler(
    IOperationalBugRepository operationalBugRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<CreateOperationalBugCommand, Guid>
{
    public async Task<Guid> Handle(CreateOperationalBugCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        var bug = OperationalBug.Create(
            request.Title,
            request.Severity,
            request.Component,
            request.Environment,
            request.Origin,
            adminId,
            request.OccurredAtUtc,
            utcNow,
            request.Description,
            request.CorrelationId,
            request.RelatedTicketId,
            request.RelatedErrorId);

        await operationalBugRepository.AddAsync(bug, cancellationToken);

        var metadataSafe =
            $"{{\"severity\":\"{bug.Severity}\",\"component\":\"{bug.Component}\",\"environment\":\"{bug.Environment}\"}}";

        await auditLogService.RecordAsync(
            AuditActions.AdminBugCreated,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.OperationalBug,
            bug.Id,
            metadataSafe,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return bug.Id;
    }
}
