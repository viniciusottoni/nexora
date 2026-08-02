using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Tickets.Commands.AddTicketNote;

/// <summary>
/// US-163: handler de adição de nota interna a um ticket de suporte.
/// Conteúdo da nota não é incluído no metadata de auditoria (apenas o resourceId é referenciado).
/// </summary>
public class AddTicketNoteCommandHandler(
    ISupportTicketRepository supportTicketRepository,
    ISupportTicketEventRepository supportTicketEventRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<AddTicketNoteCommand, Unit>
{
    public async Task<Unit> Handle(AddTicketNoteCommand request, CancellationToken cancellationToken)
    {
        var ticket = await supportTicketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportTicket), request.TicketId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        var noteEvent = SupportTicketEvent.Create(
            ticket.Id, "internal_note", null, null, request.Note, adminId, utcNow);

        await supportTicketEventRepository.AddAsync(noteEvent, cancellationToken);

        await auditLogService.RecordAsync(
            AuditActions.AdminTicketNoteAdded,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SupportTicket,
            ticket.Id,
            metadataSafe: null,
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
