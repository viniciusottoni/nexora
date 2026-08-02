using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Admin.Tickets;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Tickets.Queries.GetAdminTicketDetail;

/// <summary>
/// US-162: handler de detalhe de ticket de suporte, com histórico de eventos para triagem.
/// </summary>
public class GetAdminTicketDetailQueryHandler(
    ISupportTicketRepository supportTicketRepository,
    ISupportTicketEventRepository supportTicketEventRepository)
    : IRequestHandler<GetAdminTicketDetailQuery, AdminTicketDetailResponse>
{
    public async Task<AdminTicketDetailResponse> Handle(
        GetAdminTicketDetailQuery request, CancellationToken cancellationToken)
    {
        var ticket = await supportTicketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportTicket), request.TicketId);

        var events = await supportTicketEventRepository.GetByTicketIdAsync(request.TicketId, cancellationToken);

        var history = events
            .Select(e => new AdminTicketEventResponse(
                e.Id,
                e.EventType,
                e.OldValue,
                e.NewValue,
                e.NoteContent,
                e.AdminId,
                e.CreatedAtUtc))
            .ToList();

        return new AdminTicketDetailResponse(
            ticket.Id,
            ticket.UserId,
            ticket.Category,
            ticket.Status,
            ticket.Priority,
            ticket.AssignedAdminId,
            ticket.Language,
            ticket.Description,
            ticket.AppVersion,
            ticket.CorrelationId,
            ticket.CreatedAtUtc,
            history);
    }
}
