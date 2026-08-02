using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.RegenerateDailyQuest;

/// US-048: regenera a quest diaria respeitando o limite (RN-001/RN-003).
/// <paramref name="UseReforgeScroll"/> indica que o usuario confirmou consumir
/// 1 unidade do item "Pergaminho da Reforja" para regenerar alem do limite.
public record RegenerateDailyQuestCommand(bool UseReforgeScroll = false) : IRequest<QuestResponse>;
