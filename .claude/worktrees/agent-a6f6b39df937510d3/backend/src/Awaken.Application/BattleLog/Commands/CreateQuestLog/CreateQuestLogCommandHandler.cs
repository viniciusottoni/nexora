using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.BattleLog;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.BattleLog.Commands.CreateQuestLog;

/// US-085: cria QuestLog idempotente para uma quest. Se ja existe log para o questId,
/// retorna o existente sem duplicar XP nem historico (RN-005). O indice unico em
/// quest_logs.quest_id garante consistencia mesmo sob corrida. ADR-009/ADR-015.
public class CreateQuestLogCommandHandler(
    IQuestLogRepository questLogRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<CreateQuestLogCommandHandler> logger) : IRequestHandler<CreateQuestLogCommand, BattleLogItemResponse>
{
    public async Task<BattleLogItemResponse> Handle(
        CreateQuestLogCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var existing = await questLogRepository.GetByQuestIdAsync(request.QuestId, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "quest_log_duplicate_prevented questId={QuestId} userId={UserId} existingLogId={LogId}",
                request.QuestId, userId, existing.Id);

            return ToResponse(existing);
        }

        var log = QuestLog.Create(
            questId: request.QuestId,
            userId: userId,
            questType: request.QuestType,
            xpEarned: request.XpEarned,
            strengthXpEarned: 0,
            agilityXpEarned: 0,
            enduranceXpEarned: 0,
            vitalityXpEarned: 0,
            focusXpEarned: 0,
            wisdomXpEarned: 0,
            strengthPointsGranted: 0,
            agilityPointsGranted: 0,
            endurancePointsGranted: 0,
            vitalityPointsGranted: 0,
            focusPointsGranted: 0,
            itemsEarned: request.ItemsEarned,
            completedAtUtc: dateTimeService.UtcNow,
            xpPenaltyApplied: request.XpPenaltyApplied);

        await questLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "quest_log_created questLogId={LogId} questId={QuestId} userId={UserId} questType={Type} xpEarned={Xp}",
            log.Id, request.QuestId, userId, request.QuestType, request.XpEarned);

        return ToResponse(log);
    }

    private static BattleLogItemResponse ToResponse(QuestLog log) =>
        new(log.Id, log.QuestId, log.QuestType, log.XpEarned, log.XpPenaltyApplied, log.ItemsEarned, log.CompletedAtUtc);
}
