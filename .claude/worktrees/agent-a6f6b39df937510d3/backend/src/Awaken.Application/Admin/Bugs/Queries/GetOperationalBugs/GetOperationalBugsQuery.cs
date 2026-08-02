using Awaken.Contracts.Admin.Bugs;
using MediatR;

namespace Awaken.Application.Admin.Bugs.Queries.GetOperationalBugs;

/// <summary>
/// US-164: consulta administrativa paginada de bugs/incidentes operacionais registrados manualmente.
/// </summary>
public record GetOperationalBugsQuery(
    string? Severity,
    string? Status,
    string? Component,
    string? Environment,
    string? Origin,
    int Page,
    int PageSize) : IRequest<AdminBugListResponse>;
