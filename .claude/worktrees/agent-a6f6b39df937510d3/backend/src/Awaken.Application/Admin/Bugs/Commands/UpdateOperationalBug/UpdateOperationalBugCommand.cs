using MediatR;

namespace Awaken.Application.Admin.Bugs.Commands.UpdateOperationalBug;

/// <summary>
/// US-164: atualização de status/atribuição de um bug operacional, ou adição de comentário.
/// </summary>
public record UpdateOperationalBugCommand(
    Guid BugId,
    string? Status,
    Guid? AssignedAdminId,
    string? Comment) : IRequest<Unit>;
