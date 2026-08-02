using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.ConfirmDailyQuest;

public record ConfirmDailyQuestCommand(Guid QuestId) : IRequest<QuestResponse>;
