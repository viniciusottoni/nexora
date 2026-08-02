using Awaken.Contracts.Auth;
using MediatR;

namespace Awaken.Application.Auth.Queries.GetSession;

public record GetSessionQuery : IRequest<SessionResponse>;
