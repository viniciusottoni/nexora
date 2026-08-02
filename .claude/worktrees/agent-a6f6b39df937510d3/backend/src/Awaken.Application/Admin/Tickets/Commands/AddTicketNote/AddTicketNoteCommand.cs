using MediatR;

namespace Awaken.Application.Admin.Tickets.Commands.AddTicketNote;

/// <summary>US-163: adição de nota interna ao histórico do ticket.</summary>
public record AddTicketNoteCommand(Guid TicketId, string Note) : IRequest<Unit>;
