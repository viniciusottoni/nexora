using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.GenerateDailyQuest;

public record GenerateDailyQuestCommand : IRequest<QuestResponse>;
