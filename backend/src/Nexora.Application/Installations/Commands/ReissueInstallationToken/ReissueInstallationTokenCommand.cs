using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Installations.Commands.ReissueInstallationToken;

/// <summary>
/// POST /v1/platform/installations/{installationId}/tokens (US-156) — rotaciona o token de
/// instalação: revoga atomicamente qualquer credencial pendente anterior da MESMA instalação e
/// emite uma nova, sem duplicar tenant/loja/instalação. <paramref name="ActorId"/> é a claim
/// <c>sub</c> do administrador de plataforma (<c>ICurrentTenantContext.UserId</c>), mesmo padrão de
/// <c>TransitionTenantStatusCommand</c>.
/// </summary>
public sealed record ReissueInstallationTokenCommand(
    Guid InstallationId,
    string Reason,
    int ExpiresInHours,
    Guid? ActorId) : ICommand<ReissueInstallationTokenResponse>;
