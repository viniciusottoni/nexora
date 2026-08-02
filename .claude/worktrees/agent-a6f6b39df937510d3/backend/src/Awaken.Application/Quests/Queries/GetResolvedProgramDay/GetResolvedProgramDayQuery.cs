using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetResolvedProgramDay;

/// US-238 §18: consulta de auditoria do dia do programa resolvido pela
/// rotação cíclica sobre o histórico de treinos concluídos, para o programa
/// ativo do usuário atual.
public record GetResolvedProgramDayQuery : IRequest<ResolvedProgramDayResponse>;
