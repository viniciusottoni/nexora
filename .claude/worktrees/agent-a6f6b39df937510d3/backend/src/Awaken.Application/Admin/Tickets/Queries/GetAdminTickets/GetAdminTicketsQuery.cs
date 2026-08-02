using Awaken.Contracts.Admin.Tickets;
using MediatR;

namespace Awaken.Application.Admin.Tickets.Queries.GetAdminTickets;

/// <summary>
/// US-162: consulta administrativa paginada de tickets de suporte abertos pelo app.
/// </summary>
public record GetAdminTicketsQuery(
    string? Status,
    string? Priority,
    string? Category,
    int Page,
    int PageSize) : IRequest<AdminTicketListResponse>;
