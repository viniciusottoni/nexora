using MediatR;
using Awaken.Contracts.Auth;

namespace Awaken.Application.Auth.Commands.Login;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthResponse>;
