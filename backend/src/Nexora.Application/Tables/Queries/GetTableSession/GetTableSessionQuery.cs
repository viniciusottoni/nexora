using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Queries.GetTableSession;

/// <summary>Porta de <c>GET /v1/sessions/{id}</c> (US-022 §7).</summary>
public sealed record GetTableSessionQuery(Guid SessionId) : IQuery<TableSessionResponse>;
