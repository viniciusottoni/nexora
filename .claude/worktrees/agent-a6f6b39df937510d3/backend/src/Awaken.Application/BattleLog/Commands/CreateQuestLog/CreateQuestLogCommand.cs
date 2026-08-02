using Awaken.Contracts.BattleLog;
using MediatR;

namespace Awaken.Application.BattleLog.Commands.CreateQuestLog;

public record CreateQuestLogCommand(
    Guid QuestId,
    string QuestType,
    long XpEarned,
    IReadOnlyList<string> ItemsEarned,
    long? XpPenaltyApplied) : IRequest<BattleLogItemResponse>;
