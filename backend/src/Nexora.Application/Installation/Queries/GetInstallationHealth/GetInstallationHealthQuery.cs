using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Installation;

namespace Nexora.Application.Installation.Queries.GetInstallationHealth;

/// <summary>GET /v1/health (edge, público) — porta de <c>SystemInstallationHealthProbe</c>.</summary>
public sealed record GetInstallationHealthQuery : IQuery<InstallationHealthResponse>;
