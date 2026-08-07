using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Platform.Support;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Platform.Queries.GetAttentionQueue;

/// <summary>US-157 §"Contrato de API" — <c>GET /v1/platform/attention?severity=CRITICAL,HIGH&amp;limit=25&amp;cursor=...</c>.</summary>
public sealed record GetAttentionQueueQuery(
    IReadOnlyCollection<AttentionSeverity> Severity,
    int Limit,
    string? Cursor) : IQuery<AttentionQueueListResponse>;
