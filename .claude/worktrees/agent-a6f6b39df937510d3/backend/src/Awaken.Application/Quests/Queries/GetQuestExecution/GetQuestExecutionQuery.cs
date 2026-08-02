using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestExecution;

public record GetQuestExecutionQuery(Guid QuestId) : IRequest<QuestExecutionResponse>;
