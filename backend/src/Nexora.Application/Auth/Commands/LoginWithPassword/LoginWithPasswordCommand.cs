using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Auth;

namespace Nexora.Application.Auth.Commands.LoginWithPassword;

/// <summary>
/// Login por senha (+ MFA opcional) no cloud (gestor/administrativo) — porta de
/// PasswordAuthenticationService.login (packages/domain/src/auth/password-authentication.ts).
/// MFA é exigido quando o usuário é <c>PLATFORM_ADMIN</c> ou tem <c>MfaSecret</c> configurado.
/// </summary>
public sealed record LoginWithPasswordCommand(string Email, string Password, string? Otp) : ICommand<PasswordAuthResponse>;
