using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Installations.Queries.Platform.ListPlatformInstallations;

/// <summary>US-140 §7 — <c>GET /v1/platform/installations</c>, visão consolidada de todo o parque.</summary>
public sealed record ListPlatformInstallationsQuery : IQuery<PlatformInstallationListResponse>;
