using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestPreview;

public record GetQuestPreviewQuery(Guid QuestId) : IRequest<QuestPreviewResponse>;
