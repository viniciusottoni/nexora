using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Auth;

namespace Nexora.Application.Auth.Commands.LoginWithPin;

/// <summary>
/// Login por PIN no terminal do edge (ADR-014) — porta de AuthenticationService.loginWithPin
/// (packages/domain/src/auth/authentication-service.ts). O TenantId nunca vem do cliente: é
/// resolvido de <c>ICurrentTenantContext.TenantId</c>, fixo por instalação no edge (ADR-004).
/// </summary>
public sealed record LoginWithPinCommand(
    Guid DeviceId,
    string Pin,
    string? DeviceSecret) : ICommand<PinLoginResponse>;
