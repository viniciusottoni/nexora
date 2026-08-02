using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Bugs.Commands.UpdateOperationalBug;

/// <summary>
/// US-164: handler de atualização de bug operacional (status/atribuição/comentário).
/// Cada mudança gera um OperationalBugEvent próprio para histórico auditável.
/// Auditoria usa AdminBugClosed quando o novo status é "closed", senão AdminBugUpdated.
/// </summary>
public class UpdateOperationalBugCommandHandler(
    IOperationalBugRepository operationalBugRepository,
    IOperationalBugEventRepository operationalBugEventRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<UpdateOperationalBugCommand, Unit>
{
    public async Task<Unit> Handle(UpdateOperationalBugCommand request, CancellationToken cancellationToken)
    {
        var bug = await operationalBugRepository.GetByIdAsync(request.BugId, cancellationToken)
            ?? throw new NotFoundException(nameof(OperationalBug), request.BugId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        var oldStatus = bug.Status;
        var oldAssignedAdminId = bug.AssignedAdminId;
        var statusChanged = request.Status is not null && request.Status != oldStatus;
        var assigneeChanged = request.AssignedAdminId is not null && request.AssignedAdminId != oldAssignedAdminId;

        if (statusChanged)
        {
            bug.UpdateStatus(request.Status!, utcNow);

            var statusEvent = OperationalBugEvent.Create(
                bug.Id, "status_change", oldStatus, request.Status, null, adminId, utcNow);
            await operationalBugEventRepository.AddAsync(statusEvent, cancellationToken);
        }

        if (assigneeChanged)
        {
            bug.Assign(request.AssignedAdminId!.Value, utcNow);

            var assignedEvent = OperationalBugEvent.Create(
                bug.Id,
                "assigned",
                oldAssignedAdminId?.ToString(),
                request.AssignedAdminId?.ToString(),
                null,
                adminId,
                utcNow);
            await operationalBugEventRepository.AddAsync(assignedEvent, cancellationToken);
        }

        if (request.Comment is not null)
        {
            var commentEvent = OperationalBugEvent.Create(
                bug.Id, "comment", null, null, request.Comment, adminId, utcNow);
            await operationalBugEventRepository.AddAsync(commentEvent, cancellationToken);
        }

        if (statusChanged || assigneeChanged || request.Comment is not null)
        {
            var action = request.Status == "closed" ? AuditActions.AdminBugClosed : AuditActions.AdminBugUpdated;

            await auditLogService.RecordAsync(
                action,
                adminId,
                AuditActorType.Admin,
                AuditResourceTypes.OperationalBug,
                bug.Id,
                metadataSafe: $"{{\"statusChanged\":{statusChanged.ToString().ToLowerInvariant()},\"assigneeChanged\":{assigneeChanged.ToString().ToLowerInvariant()}}}",
                cancellationToken: cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
