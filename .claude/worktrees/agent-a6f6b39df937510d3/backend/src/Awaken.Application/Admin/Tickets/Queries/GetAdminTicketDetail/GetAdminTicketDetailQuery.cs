using Awaken.Contracts.Admin.Tickets;
using MediatR;

namespace Awaken.Application.Admin.Tickets.Queries.GetAdminTicketDetail;

/// <summary>
/// US-162: consulta de detalhe de um ticket de suporte, incluindo histórico de eventos.
/// </summary>
public record GetAdminTicketDetailQuery(Guid TicketId) : IRequest<AdminTicketDetailResponse>;
