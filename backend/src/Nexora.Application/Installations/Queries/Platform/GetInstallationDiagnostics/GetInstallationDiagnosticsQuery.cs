using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Installations.Queries.Platform.GetInstallationDiagnostics;

/// <summary>US-140 §7 — <c>GET /v1/platform/installations/{id}/diagnostics</c> (diagnóstico remoto: health check + logs recentes, RN-015).</summary>
public sealed record GetInstallationDiagnosticsQuery(Guid InstallationId) : IQuery<InstallationDiagnosticsResponse>;
