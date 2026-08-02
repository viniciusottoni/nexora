using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.PauseQuest;

public record PauseQuestCommand(Guid QuestId) : IRequest<PauseQuestResponse>;
