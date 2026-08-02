using Awaken.Contracts.Admin.Tickets;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Tickets.Queries.GetAdminTickets;

/// <summary>
/// US-162: handler de listagem paginada de tickets de suporte para o site admin.
/// Descrição é truncada em 120 caracteres na visão de lista.
/// </summary>
public class GetAdminTicketsQueryHandler(ISupportTicketRepository supportTicketRepository)
    : IRequestHandler<GetAdminTicketsQuery, AdminTicketListResponse>
{
    private const int DescriptionPreviewLength = 120;

    public async Task<AdminTicketListResponse> Handle(
        GetAdminTicketsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await supportTicketRepository.GetPagedAsync(
            request.Status,
            request.Priority,
            request.Category,
            request.Page,
            request.PageSize,
            cancellationToken);

        var projected = items
            .Select(t => new AdminTicketSummaryResponse(
                t.Id,
                t.UserId,
                t.Category,
                t.Status,
                t.Priority,
                t.AssignedAdminId,
                Truncate(t.Description, DescriptionPreviewLength),
                t.CreatedAtUtc))
            .ToList();

        return new AdminTicketListResponse(projected, total, request.Page, request.PageSize);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
