using MediatR;

namespace Awaken.Application.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Unit>;
