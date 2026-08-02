using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Support;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Support.Commands.CreateSupportTicket;

public class CreateSupportTicketCommandHandler(
    ICurrentUserService currentUserService,
    ISupportTicketRepository supportTicketRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<CreateSupportTicketCommandHandler> logger) : IRequestHandler<CreateSupportTicketCommand, SupportTicketResponse>
{
    public async Task<SupportTicketResponse> Handle(
        CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        var ticket = SupportTicket.Create(
            userId,
            request.Category,
            request.Language,
            request.Description,
            request.AppVersion,
            request.CorrelationId,
            utcNow);

        await supportTicketRepository.AddAsync(ticket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Support ticket {TicketId} created for user {UserId} with category {Category}",
            ticket.Id, userId, ticket.Category);

        return new SupportTicketResponse(ticket.Id, ticket.Status);
    }
}
