using MediatR;

namespace Awaken.Application.Admin.Tickets.Commands.TriageTicket;

/// <summary>
/// US-163: triagem de ticket — atualização de status, prioridade e/ou responsável.
/// RN-005: status deve ser um dos valores controlados do fluxo.
/// </summary>
public record TriageTicketCommand(
    Guid TicketId,
    string? Status,
    string? Priority,
    Guid? AssignedAdminId) : IRequest<Unit>;
