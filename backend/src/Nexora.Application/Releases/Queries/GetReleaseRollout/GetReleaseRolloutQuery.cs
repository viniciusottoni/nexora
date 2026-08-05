using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Releases.Queries.GetReleaseRollout;

/// <summary>GET /v1/platform/releases/{version}/rollout (US-146 §7/§10 "Progresso da liberação visível no painel de plataforma").</summary>
public sealed record GetReleaseRolloutQuery(string Version) : IQuery<ReleaseRolloutResponse>;
