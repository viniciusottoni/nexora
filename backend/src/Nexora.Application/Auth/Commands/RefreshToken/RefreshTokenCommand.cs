using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Auth;

namespace Nexora.Application.Auth.Commands.RefreshToken;

/// <summary>Renovação de sessão do cloud — porta de RefreshAuthService.execute (apps/api-cloud/src/modules/auth/refresh-auth.service.ts). Rotaciona o refresh token a cada uso.</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<PasswordAuthResponse>;
