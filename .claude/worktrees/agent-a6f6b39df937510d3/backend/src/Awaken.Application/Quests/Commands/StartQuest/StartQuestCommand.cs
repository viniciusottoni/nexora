using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.StartQuest;

public record StartQuestCommand(Guid QuestId) : IRequest<StartQuestResponse>;
