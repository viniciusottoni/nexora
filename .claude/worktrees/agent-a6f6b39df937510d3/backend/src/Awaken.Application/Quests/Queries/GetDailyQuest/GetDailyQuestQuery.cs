using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetDailyQuest;

public record GetDailyQuestQuery : IRequest<QuestResponse>;
