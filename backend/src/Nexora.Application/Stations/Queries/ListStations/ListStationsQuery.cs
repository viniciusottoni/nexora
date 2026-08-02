using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Stations;

namespace Nexora.Application.Stations.Queries.ListStations;

/// <summary>Porta de <c>GET /v1/catalog/stations</c> — lista as praças de produção do tenant autenticado.</summary>
public sealed record ListStationsQuery : IQuery<StationListResponse>;
