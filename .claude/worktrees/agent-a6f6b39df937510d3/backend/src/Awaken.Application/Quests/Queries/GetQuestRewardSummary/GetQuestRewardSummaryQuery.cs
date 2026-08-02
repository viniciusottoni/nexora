using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestRewardSummary;

public record GetQuestRewardSummaryQuery(Guid QuestId) : IRequest<QuestRewardSummaryResponse>;
