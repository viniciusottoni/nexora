using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Queries.GetKdsQueue;

/// <summary>
/// Porta de <c>GET /v1/kds/queue?stationId=...&amp;since=...</c> (US-031 §7) — fallback de polling
/// do ADR-011 (a cada 5 s no cliente) e também a fonte reaproveitada por <c>KdsHub.Resume</c> na
/// reconexão (ver docstring do handler para a decisão "snapshot completo, não delta").
/// <paramref name="Since"/> é aceito por compatibilidade de contrato com o ADR-011 mas não filtra o
/// resultado nesta implementação.
/// </summary>
public sealed record GetKdsQueueQuery(Guid StationId, string? Since) : IQuery<GetKdsQueueResponse>;
