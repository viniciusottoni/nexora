using Awaken.Contracts.Admin.Performance;
using MediatR;

namespace Awaken.Application.Admin.Performance.Queries.GetPerformanceOverview;

/// <summary>
/// US-220: consulta administrativa de performance agregada (API, banco, Redis, caches).
/// Environment e período são filtros de contexto — hoje aplicados apenas ao retorno
/// (rotulagem/segmentação), já que não há fonte de série histórica persistida (RN-002).
/// </summary>
public record GetPerformanceOverviewQuery(
    string? Environment,
    DateTime? From,
    DateTime? To) : IRequest<PerformanceOverviewResponse>;
