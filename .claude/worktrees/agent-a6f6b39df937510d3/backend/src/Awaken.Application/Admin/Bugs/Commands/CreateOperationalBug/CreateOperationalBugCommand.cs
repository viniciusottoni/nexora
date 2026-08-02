using MediatR;

namespace Awaken.Application.Admin.Bugs.Commands.CreateOperationalBug;

/// <summary>
/// US-171: registro manual de um bug/incidente interno pelo time admin.
/// </summary>
public record CreateOperationalBugCommand(
    string Title,
    string Severity,
    string Component,
    string Environment,
    string Origin,
    DateTime OccurredAtUtc,
    string? Description,
    string? CorrelationId,
    Guid? RelatedTicketId,
    string? RelatedErrorId) : IRequest<Guid>;
