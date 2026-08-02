using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.ResumeQuest;

public record ResumeQuestCommand(Guid QuestId) : IRequest<ResumeQuestResponse>;
