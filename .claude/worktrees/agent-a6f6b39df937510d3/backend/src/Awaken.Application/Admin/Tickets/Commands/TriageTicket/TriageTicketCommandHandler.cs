using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Tickets.Commands.TriageTicket;

/// <summary>
/// US-163: handler de triagem de ticket de suporte.
/// RN-005: status segue fluxo controlado (validado no validator).
/// Cada campo alterado gera um SupportTicketEvent próprio para histórico auditável.
/// </summary>
public class TriageTicketCommandHandler(
    ISupportTicketRepository supportTicketRepository,
    ISupportTicketEventRepository supportTicketEventRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<TriageTicketCommand, Unit>
{
    public async Task<Unit> Handle(TriageTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await supportTicketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportTicket), request.TicketId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        var oldStatus = ticket.Status;
        var oldPriority = ticket.Priority;
        var oldAssignedAdminId = ticket.AssignedAdminId;

        if (request.Status is not null && request.Status != oldStatus)
        {
            ticket.UpdateStatus(request.Status, utcNow);

            var statusEvent = SupportTicketEvent.Create(
                ticket.Id, "status_change", oldStatus, request.Status, null, adminId, utcNow);
            await supportTicketEventRepository.AddAsync(statusEvent, cancellationToken);

            await auditLogService.RecordAsync(
                AuditActions.AdminTicketStatusChanged,
                adminId,
                AuditActorType.Admin,
                AuditResourceTypes.SupportTicket,
                ticket.Id,
                metadataSafe: $"{{\"oldStatus\":\"{oldStatus}\",\"newStatus\":\"{request.Status}\"}}",
                cancellationToken: cancellationToken);
        }

        var priorityChanged = request.Priority is not null && request.Priority != oldPriority;
        var assigneeChanged = request.AssignedAdminId is not null && request.AssignedAdminId != oldAssignedAdminId;

        if (priorityChanged || assigneeChanged)
        {
            var newPriority = request.Priority ?? ticket.Priority;
            var newAssignedAdminId = request.AssignedAdminId ?? ticket.AssignedAdminId;

            ticket.UpdateTriage(newPriority, newAssignedAdminId, utcNow);

            if (priorityChanged)
            {
                var priorityEvent = SupportTicketEvent.Create(
                    ticket.Id, "priority_change", oldPriority, request.Priority, null, adminId, utcNow);
                await supportTicketEventRepository.AddAsync(priorityEvent, cancellationToken);
            }

            if (assigneeChanged)
            {
                var assignedEvent = SupportTicketEvent.Create(
                    ticket.Id,
                    "assigned",
                    oldAssignedAdminId?.ToString(),
                    request.AssignedAdminId?.ToString(),
                    null,
                    adminId,
                    utcNow);
                await supportTicketEventRepository.AddAsync(assignedEvent, cancellationToken);
            }

            await auditLogService.RecordAsync(
                AuditActions.AdminTicketTriaged,
                adminId,
                AuditActorType.Admin,
                AuditResourceTypes.SupportTicket,
                ticket.Id,
                metadataSafe: $"{{\"priorityChanged\":{priorityChanged.ToString().ToLowerInvariant()},\"assigneeChanged\":{assigneeChanged.ToString().ToLowerInvariant()}}}",
                cancellationToken: cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
