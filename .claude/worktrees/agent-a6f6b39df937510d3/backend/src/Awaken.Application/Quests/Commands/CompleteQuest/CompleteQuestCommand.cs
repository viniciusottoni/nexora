using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.CompleteQuest;

/// US-241 §6.2: <paramref name="PerceivedFeeling"/> é a pergunta simples feita ao
/// usuário na conclusão ("como foi esse treino?") — único sinal real de desempenho
/// usado pela autorregulação da progressão semanal. Ver <see cref="Awaken.Domain.Entities.Quests.PerceivedFeelings"/>.
public record CompleteQuestCommand(Guid QuestId, string? PerceivedFeeling = null) : IRequest<CompleteQuestResponse>;
