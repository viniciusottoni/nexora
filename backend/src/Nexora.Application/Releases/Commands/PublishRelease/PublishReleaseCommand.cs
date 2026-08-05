using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Releases.Commands.PublishRelease;

/// <summary>
/// POST /v1/platform/releases (US-146 §7) — publica uma versão nova OU amplia a liberação
/// gradual de uma versão já publicada (ver docstring do handler para a regra de re-publicação).
/// </summary>
public sealed record PublishReleaseCommand(string Version, int RolloutPercent, string? Notes)
    : ICommand<PublishReleaseResponse>;
