using Awaken.Contracts.Admin.Routines;
using MediatR;

namespace Awaken.Application.Admin.Routines.Queries.GetRoutinesOverview;

/// <summary>
/// US-221: consulta de visão operacional de rotinas, workers, filas e atualizações operacionais.
/// Sem parâmetros — sempre retorna o estado atual completo.
/// </summary>
public record GetRoutinesOverviewQuery : IRequest<RoutinesOverviewResponse>;
