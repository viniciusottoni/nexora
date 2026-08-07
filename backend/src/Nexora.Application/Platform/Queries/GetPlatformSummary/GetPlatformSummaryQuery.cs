using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Platform.Queries.GetPlatformSummary;

/// <summary>US-150 §7 — <c>GET /v1/platform/summary</c>, resumo consumido pela visão geral do shell da plataforma.</summary>
public sealed record GetPlatformSummaryQuery : IQuery<PlatformSummaryResponse>;
