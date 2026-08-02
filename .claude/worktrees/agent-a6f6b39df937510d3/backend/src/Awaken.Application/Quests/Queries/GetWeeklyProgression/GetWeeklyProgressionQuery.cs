using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetWeeklyProgression;

/// US-241 §18: consulta o plano de progressão semanal vigente para o usuário atual.
public record GetWeeklyProgressionQuery : IRequest<WeeklyProgressionResponse>;
