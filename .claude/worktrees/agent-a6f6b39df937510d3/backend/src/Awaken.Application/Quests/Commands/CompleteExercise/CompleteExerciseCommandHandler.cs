using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Services;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Quests.Commands.CompleteExercise;

/// US-064/US-065: marca um QuestExercise como concluído, calculando XP proporcional (US-065)
/// e concedendo de forma idempotente (US-064).
/// US-154/US-155: aplica diminishing returns e proteção anti-abuso ao ganho de RankScore.
public class CompleteExerciseCommandHandler(
    IQuestRepository questRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IRankScoreLogRepository rankScoreLogRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<CompleteExerciseCommandHandler> logger) : IRequestHandler<CompleteExerciseCommand, CompleteExerciseResponse>
{
    public async Task<CompleteExerciseResponse> Handle(
        CompleteExerciseCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdWithExercisesAsync(request.QuestId, cancellationToken);

        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        if (quest.Status != "in_progress")
            throw new ConflictException("QUEST_NOT_IN_PROGRESS", "Quest nao esta em andamento.");

        var exercise = quest.Exercises.FirstOrDefault(e => e.Id == request.QuestExerciseId)
            ?? throw new NotFoundException("QuestExercise", request.QuestExerciseId);

        var projection = QuestExerciseRewardMapper.Project(exercise);

        // US-065: calcula XP proporcional ao grau de conclusão e restrição de dor.
        var calculatedXp = XpCalculationService.CalculateExerciseXp(
            exercise.XpReward,
            exercise.Sets,
            request.SetsCompleted,
            request.StrongPainReported);

        var awarded = exercise.MarkCompleted(dateTimeService.UtcNow, calculatedXp);

        long totalXp = 0;
        var levelsGained = 0;
        var rankChanged = false;
        string? newRank = null;
        IReadOnlyList<string> attributeLevelUps = [];
        var actualLevelUps = new QuestVisibleAttributeImpactsDto(0, 0, 0, 0, 0);

        if (awarded)
        {
            logger.LogInformation(
                "xp_earned source=quest questType={QuestType} amount={Amount} questId={QuestId} questExerciseId={QuestExerciseId} userId={UserId}",
                quest.Type, exercise.XpEarned, quest.Id, exercise.Id, quest.UserId);

            var progression = await hunterProgressionRepository.GetByUserIdAsync(quest.UserId, cancellationToken);
            if (progression is not null)
            {
                var levelBefore = progression.Level;
                var rankBefore = progression.Rank;

                progression.AddXp(exercise.XpEarned, dateTimeService.UtcNow);

                // US-155: avalia contexto de abuso antes de aplicar RankScore.
                var abuseEval = RankScoreAbuseProtectionService.Evaluate(
                    request.StrongPainReported,
                    request.SetsCompleted,
                    exercise.Sets);

                if (abuseEval.AbuseSuspected)
                    logger.LogInformation(
                        "rank_abuse_suspected userId={UserId} multiplier={Multiplier}",
                        quest.UserId, abuseEval.Multiplier);

                // US-130/US-154: XP interno por atributo acumula no buffer; RankScore com DR.
                var addResult = progression.AddAttributeXp(
                    projection.AttributeXpEarned.Strength,
                    projection.AttributeXpEarned.Agility,
                    projection.AttributeXpEarned.Endurance,
                    projection.AttributeXpEarned.Vitality,
                    projection.AttributeXpEarned.Focus,
                    projection.AttributeXpEarned.Wisdom,
                    externalMultiplier: abuseEval.Multiplier,
                    utcNow: dateTimeService.UtcNow);

                var levelUps = addResult.LevelUps;
                var rankScoreAudit = addResult.RankScoreAudit;

                exercise.SetAttributeLevelUps(
                    levelUps.Strength, levelUps.Agility, levelUps.Endurance,
                    levelUps.Vitality, levelUps.Focus, levelUps.Wisdom);

                levelsGained = progression.Level - levelBefore;
                rankChanged = progression.Rank != rankBefore;
                newRank = rankChanged ? progression.Rank : null;
                totalXp = progression.TotalXp;
                attributeLevelUps = levelUps.ToNameList();
                actualLevelUps = new QuestVisibleAttributeImpactsDto(
                    levelUps.Strength, levelUps.Agility, levelUps.Endurance,
                    levelUps.Vitality, levelUps.Focus);

                // US-154/US-155: registra auditoria de RankScore quando houve ganho bruto ou abuso.
                if (rankScoreAudit.RawGain > 0 || abuseEval.AbuseSuspected)
                {
                    var log = RankScoreLog.Create(
                        quest.UserId,
                        source: "quest_exercise",
                        rawGain: rankScoreAudit.RawGain,
                        multiplier: rankScoreAudit.Multiplier,
                        externalMultiplier: rankScoreAudit.ExternalMultiplier,
                        effectiveGain: rankScoreAudit.EffectiveGain,
                        wasMonthlyLimitApplied: rankScoreAudit.WasMonthlyLimitApplied,
                        wasAbuseSuspected: abuseEval.AbuseSuspected,
                        rankScoreAfter: progression.RankScore);

                    await rankScoreLogRepository.AddAsync(log, cancellationToken);
                }

                if (rankScoreAudit.Multiplier < 1.0m && rankScoreAudit.RawGain > 0)
                    logger.LogInformation(
                        "rank_diminishing_returns_applied userId={UserId} multiplier={Multiplier} rawGain={Raw} effectiveGain={Effective}",
                        quest.UserId, rankScoreAudit.Multiplier, rankScoreAudit.RawGain, rankScoreAudit.EffectiveGain);

                if (rankScoreAudit.WasMonthlyLimitApplied)
                    logger.LogInformation(
                        "rank_progress_monthly_limit_reached userId={UserId}",
                        quest.UserId);

                if (levelsGained > 0)
                {
                    logger.LogInformation(
                        "level_up source=quest userId={UserId} questId={QuestId} newLevel={NewLevel}",
                        quest.UserId, quest.Id, progression.Level);
                }

                if (rankChanged)
                {
                    logger.LogInformation(
                        "rank_up source=quest userId={UserId} questId={QuestId} newRank={NewRank}",
                        quest.UserId, quest.Id, newRank);
                }
            }

            questRepository.Update(quest);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Já concluído: retorna o TotalXp atual sem alterar.
            var progression = await hunterProgressionRepository.GetByUserIdAsync(quest.UserId, cancellationToken);
            totalXp = progression?.TotalXp ?? 0;
        }

        return new CompleteExerciseResponse(
            QuestExerciseId: exercise.Id,
            Status: exercise.Status,
            XpEarned: exercise.XpEarned,
            TotalXp: totalXp,
            EffectiveDifficulty: projection.EffectiveDifficulty,
            AttributeXpEarned: projection.AttributeXpEarned,
            AttributePointsGranted: actualLevelUps,
            AlreadyCompleted: !awarded,
            LevelsGained: levelsGained,
            RankChanged: rankChanged,
            NewRank: newRank,
            AttributeLevelUps: attributeLevelUps);
    }
}
