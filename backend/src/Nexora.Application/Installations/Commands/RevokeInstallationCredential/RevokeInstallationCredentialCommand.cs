using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Installations.Commands.RevokeInstallationCredential;

/// <summary>
/// DELETE /v1/platform/installations/{installationId}/tokens/{credentialId} (US-156) — revogação
/// manual de uma credencial de instalação comprometida, independentemente de ela ser a mais recente
/// ou uma linha histórica. <paramref name="ActorId"/> é a claim <c>sub</c> do administrador de
/// plataforma (<c>ICurrentTenantContext.UserId</c>).
/// </summary>
public sealed record RevokeInstallationCredentialCommand(
    Guid InstallationId,
    Guid CredentialId,
    string Reason,
    Guid? ActorId) : ICommand;
