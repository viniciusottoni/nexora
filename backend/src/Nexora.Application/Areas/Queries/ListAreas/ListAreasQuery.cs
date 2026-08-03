using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Areas.Queries.ListAreas;

/// <summary>Lista os ambientes do tenant autenticado (todas as lojas do usuário). Porta de <c>GET /v1/areas</c>.</summary>
public sealed record ListAreasQuery : IQuery<AreaListResponse>;
