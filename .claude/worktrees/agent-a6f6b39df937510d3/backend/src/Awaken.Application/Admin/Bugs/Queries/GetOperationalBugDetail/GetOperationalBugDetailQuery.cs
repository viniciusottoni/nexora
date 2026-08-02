using Awaken.Contracts.Admin.Bugs;
using MediatR;

namespace Awaken.Application.Admin.Bugs.Queries.GetOperationalBugDetail;

/// <summary>
/// US-164: consulta de detalhe de um bug operacional, incluindo histórico de eventos.
/// </summary>
public record GetOperationalBugDetailQuery(Guid BugId) : IRequest<AdminBugDetailResponse>;
