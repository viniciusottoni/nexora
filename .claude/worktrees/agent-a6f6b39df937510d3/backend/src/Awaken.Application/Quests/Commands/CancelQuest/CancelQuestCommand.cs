using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.CancelQuest;

public record CancelQuestCommand(Guid QuestId) : IRequest<CancelQuestResponse>;
