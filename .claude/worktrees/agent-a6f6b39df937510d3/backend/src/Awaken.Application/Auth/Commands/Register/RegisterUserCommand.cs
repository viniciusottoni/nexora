using MediatR;
using Awaken.Contracts.Auth;

namespace Awaken.Application.Auth.Commands.Register;

public record RegisterUserCommand(string Email, string Password, string? DisplayName, string Language = "pt-BR")
    : IRequest<AuthResponse>;
