using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Installations;

namespace Nexora.Application.Installations.Commands.RegisterInstallation;

/// <summary>
/// POST /v1/platform/installations/register (cloud) — porta de <c>RegisterInstallationService</c>
/// + <c>PrismaInstallationRegistrationRepository.register/findCompleted</c>. O token cru vem do
/// header <c>X-Install-Token</c> (nunca logado; só o hash é comparado/persistido).
/// </summary>
public sealed record RegisterInstallationCommand(
    string RawToken,
    Guid InstallationId,
    string Hostname,
    string Version,
    string PublicKey) : ICommand<RegisterInstallationResponse>;
