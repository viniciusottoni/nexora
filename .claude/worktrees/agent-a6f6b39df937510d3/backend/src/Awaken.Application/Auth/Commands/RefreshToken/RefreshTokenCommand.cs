using MediatR;
using Awaken.Contracts.Auth;

namespace Awaken.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
