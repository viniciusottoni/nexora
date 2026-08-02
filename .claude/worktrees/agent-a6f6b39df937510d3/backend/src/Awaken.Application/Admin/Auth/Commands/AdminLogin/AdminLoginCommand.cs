using Awaken.Contracts.Admin.Auth;
using MediatR;

namespace Awaken.Application.Admin.Auth.Commands.AdminLogin;

public record AdminLoginCommand(string Email, string Password) : IRequest<AdminLoginResponse>;
