using MediatR;
using Awaken.Contracts.Auth;

namespace Awaken.Application.Auth.Commands.GoogleSignIn;

public record GoogleSignInCommand(string Provider, string ProviderCredential) : IRequest<AuthResponse>;
