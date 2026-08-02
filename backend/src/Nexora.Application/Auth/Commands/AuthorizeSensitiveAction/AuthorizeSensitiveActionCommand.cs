using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Auth;

namespace Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;

/// <summary>
/// Elevação pontual (ADR-023) — porta de SensitiveAuthorizationService.authorize
/// (apps/api-edge/src/modules/auth/sensitive-authorization.service.ts). Tenant/loja/ator/
/// dispositivo vêm de <c>ICurrentTenantContext</c> (sessão operacional já autenticada do
/// operador que está pedindo a autorização) — nunca do corpo da requisição.
/// </summary>
public sealed record AuthorizeSensitiveActionCommand(
    string Action,
    string Pin,
    IReadOnlyDictionary<string, object?> Context) : ICommand<AuthorizeSensitiveActionResponse>;
