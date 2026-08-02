using Awaken.Contracts.Support;
using MediatR;

namespace Awaken.Application.Support.Commands.CreateSupportTicket;

public record CreateSupportTicketCommand(
    string Category,
    string Description,
    string? AppVersion,
    string? CorrelationId,
    string Language) : IRequest<SupportTicketResponse>;
