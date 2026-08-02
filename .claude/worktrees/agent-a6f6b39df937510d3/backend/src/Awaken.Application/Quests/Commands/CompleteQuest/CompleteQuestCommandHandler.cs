using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Training;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Quests.Commands.CompleteQuest;

/// US-061/US-062: conclui a execucao de uma quest (daily/dungeon/raid), consolidando
/// o resultado final ja acumulado pelos exercicios concluidos, atualizando o streak
/// e registrando o QuestLog de forma idempotente (RN-003/RN-007).
/// US-154: bônus de streak também sujeito a diminishing returns.
public class CompleteQuestCommandHandler(
    IQuestRepository questRepository,
    IQuestLogRepository questLogRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IInventoryRepository inventoryRepository,
    IRankScoreLogRepository rankScoreLogRepository,
    IItemActiveEffectRepository itemActiveEffectRepository,
    IMuscleRecoveryStateRepository muscleRecoveryStateRepository,
    IExerciseCatalogRepository exerciseCatalogRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<CompleteQuestCommandHandler> logger) : IRequestHandler<CompleteQuestCommand, CompleteQuestResponse>
{
    public async Task<CompleteQuestResponse> Handle(
        CompleteQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdWithExercisesAsync(request.QuestId, cancellationToken);

        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        // RN-003/9.1: quest ja concluida retorna o resultado idempotente sem duplicar recompensa.
        if (quest.Status == "completed")
        {
            var existingLog = await questLogRepository.GetByQuestIdAsync(quest.Id, cancellationToken);
            logger.LogInformation(
                "quest_log_duplicate_prevented questId={QuestId} userId={UserId}",
                quest.Id, userId);
            return BuildResponse(quest, existingLog);
        }

        // RN-001/RN-002/9.2: apenas quest em andamento ou pausada pode ser concluida.
        if (quest.Status is not ("in_progress" or "paused"))
            throw new ConflictException("QUEST_NOT_COMPLETABLE", "Quest nao esta em um estado concluivel.");

        var completedAtUtc = dateTimeService.UtcNow;

        var totalXpEarned = quest.Exercises.Sum(e => e.XpEarned);
        var attributeXpEarned = QuestExerciseRewardMapper.Summarize(quest.Exercises);
        // US-130: soma dos level-ups reais por exercício (não mais projeção).
        var attributePointsGranted = SumExerciseLevelUps(quest.Exercises);

        // RN-007/RN-EPIC-008-008: dungeon concede item ao ser concluida.
        var itemsEarned = new List<string>();
        if (quest.Type == "dungeon")
        {
            await GrantDungeonItemAsync(quest.UserId, itemsEarned, completedAtUtc, cancellationToken);
            foreach (var itemKey in itemsEarned)
            {
                logger.LogInformation(
                    "item_earned itemKey={ItemKey} userId={UserId} source=dungeon questType={Type}",
                    itemKey, quest.UserId, quest.Type);
            }
        }

        var progression = await hunterProgressionRepository.GetByUserIdAsync(quest.UserId, cancellationToken);
        if (progression is not null)
        {
            // US-230/P0-1: Poção de Foco/Grande boosta o TOTAL do treino, não um
            // exercício isolado — o XP base já foi creditado por exercício em
            // CompleteExerciseCommandHandler, então o boost é um bônus adicional
            // creditado agora, sobre o total do treino que acabou de fechar.
            var xpBoostBonus = await TryConsumeXpBoostAsync(quest.UserId, totalXpEarned, cancellationToken);
            if (xpBoostBonus > 0)
            {
                totalXpEarned += xpBoostBonus;
                progression.AddXp(xpBoostBonus, completedAtUtc);
                logger.LogInformation(
                    "xp_boost_applied userId={UserId} bonusXp={Bonus} questId={QuestId}",
                    quest.UserId, xpBoostBonus, quest.Id);
            }
        }

        quest.Complete(totalXpEarned, completedAtUtc);
        questRepository.Update(quest);

        // US-239/RN-008: atualiza o estado de recuperação muscular dos grupos
        // trabalhados — só roda no fechamento real da quest (não no replay
        // idempotente acima), garantindo que quest não concluída não gera fadiga.
        await UpdateMuscleRecoveryStateAsync(quest, completedAtUtc, cancellationToken);

        if (progression is not null)
        {
            // US-230: Amuleto de Retorno — se elegível, restaura o streak perdido
            // ontem em vez de tratar hoje como um novo streak do zero.
            var streakRecoveryDays = await TryConsumeStreakRecoveryAsync(
                quest.UserId, completedAtUtc, cancellationToken);
            if (streakRecoveryDays is { } daysToRestore)
            {
                progression.RestoreStreak(daysToRestore + 1, completedAtUtc);
                logger.LogInformation(
                    "streak_recovered userId={UserId} streakDays={Days}",
                    quest.UserId, progression.CurrentStreakDays);
            }
            else
            {
                progression.UpdateStreakAfterQuestCompletion(completedAtUtc);
            }

            logger.LogInformation(
                "streak_updated userId={UserId} streakDays={Days}",
                quest.UserId, progression.CurrentStreakDays);

            // US-069/US-154: bônus de RankScore em marcos de streak, sujeito a DR.
            var milestoneResult = progression.TryApplyStreakMilestoneBonus(completedAtUtc);
            if (milestoneResult.RawBonus > 0)
            {
                logger.LogInformation(
                    "rank_streak_bonus_applied userId={UserId} rawBonus={Bonus} effectiveGain={Effective} streakDays={Days}",
                    quest.UserId, milestoneResult.RawBonus,
                    milestoneResult.RankScoreAudit?.EffectiveGain ?? 0,
                    progression.CurrentStreakDays);

                if (milestoneResult.RankScoreAudit is { } audit)
                {
                    var log = RankScoreLog.Create(
                        quest.UserId,
                        source: "streak_bonus",
                        rawGain: audit.RawGain,
                        multiplier: audit.Multiplier,
                        externalMultiplier: audit.ExternalMultiplier,
                        effectiveGain: audit.EffectiveGain,
                        wasMonthlyLimitApplied: audit.WasMonthlyLimitApplied,
                        wasAbuseSuspected: false,
                        rankScoreAfter: progression.RankScore);

                    await rankScoreLogRepository.AddAsync(log, cancellationToken);
                }
            }
        }

        var questLog = QuestLog.Create(
            quest.Id,
            quest.UserId,
            quest.Type,
            totalXpEarned,
            strengthXpEarned: attributeXpEarned.Strength,
            agilityXpEarned: attributeXpEarned.Agility,
            enduranceXpEarned: attributeXpEarned.Endurance,
            vitalityXpEarned: attributeXpEarned.Vitality,
            focusXpEarned: attributeXpEarned.Focus,
            wisdomXpEarned: attributeXpEarned.Wisdom,
            strengthPointsGranted: attributePointsGranted.Strength,
            agilityPointsGranted: attributePointsGranted.Agility,
            endurancePointsGranted: attributePointsGranted.Endurance,
            vitalityPointsGranted: attributePointsGranted.Vitality,
            focusPointsGranted: attributePointsGranted.Focus,
            itemsEarned: itemsEarned,
            completedAtUtc: completedAtUtc,
            perceivedFeeling: request.PerceivedFeeling);

        await questLogRepository.AddAsync(questLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "quest_log_created questLogId={LogId} questId={QuestId} userId={UserId} questType={Type} xpEarned={Xp}",
            questLog.Id, quest.Id, quest.UserId, quest.Type, totalXpEarned);

        if (attributePointsGranted.Strength > 0)
            logger.LogInformation("attribute_level_up attribute=strength userId={UserId} questType={Type}", quest.UserId, quest.Type);
        if (attributePointsGranted.Agility > 0)
            logger.LogInformation("attribute_level_up attribute=agility userId={UserId} questType={Type}", quest.UserId, quest.Type);
        if (attributePointsGranted.Endurance > 0)
            logger.LogInformation("attribute_level_up attribute=endurance userId={UserId} questType={Type}", quest.UserId, quest.Type);
        if (attributePointsGranted.Vitality > 0)
            logger.LogInformation("attribute_level_up attribute=vitality userId={UserId} questType={Type}", quest.UserId, quest.Type);
        if (attributePointsGranted.Focus > 0)
            logger.LogInformation("attribute_level_up attribute=focus userId={UserId} questType={Type}", quest.UserId, quest.Type);

        return BuildResponse(quest, questLog);
    }

    private async Task GrantDungeonItemAsync(
        Guid userId, List<string> itemsEarned, DateTime utcNow, CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryRepository.GetByUserIdAndItemKeyAsync(
            userId, ItemKeys.DungeonStone, cancellationToken);

        if (inventoryItem is null)
        {
            inventoryItem = InventoryItem.Create(userId, ItemKeys.DungeonStone);
            await inventoryRepository.AddAsync(inventoryItem, cancellationToken);
        }

        inventoryItem.Add(1, utcNow);
        itemsEarned.Add(ItemKeys.DungeonStone);
    }

    /// US-239: para cada exercício concluído com catálogo resolvido, registra a
    /// sessão nos grupos musculares (primários + secundários) trabalhados,
    /// com a intensidade inferida (<see cref="RecoveryPlanner.IntensityFor"/>)
    /// e a movementFamily do exercício (anti-repetição, RN-007). Exercícios de
    /// fallback (sem <c>ExerciseCatalogProviderId</c> ou sem catálogo resolvido)
    /// são ignorados sem derrubar o fluxo de conclusão.
    private async Task UpdateMuscleRecoveryStateAsync(
        Quest quest, DateTime completedAtUtc, CancellationToken cancellationToken)
    {
        var updatedMuscleGroups = new HashSet<string>(StringComparer.Ordinal);

        foreach (var exercise in quest.Exercises.Where(e => e.Status == "completed"))
        {
            if (string.IsNullOrWhiteSpace(exercise.ExerciseCatalogProviderId))
                continue;

            var catalog = await exerciseCatalogRepository.GetByProviderExerciseIdAsync(
                exercise.ExerciseCatalogProviderId, cancellationToken);
            if (catalog is null)
                continue;

            var intensity = RecoveryPlanner.IntensityFor(catalog);
            var movementFamilies = string.IsNullOrWhiteSpace(catalog.MovementFamily)
                ? []
                : new List<string> { catalog.MovementFamily };

            var muscleGroups = catalog.PrimaryMuscleGroups
                .Concat(catalog.SecondaryMuscleGroups)
                .Distinct();

            foreach (var muscleGroup in muscleGroups)
            {
                var state = await muscleRecoveryStateRepository.GetByUserAndMuscleGroupAsync(
                    quest.UserId, muscleGroup, cancellationToken);

                if (state is null)
                {
                    state = MuscleRecoveryState.CreateInitial(quest.UserId, muscleGroup, completedAtUtc);
                    state.RegisterSession(completedAtUtc, intensity, movementFamilies, exercise.Sets);
                    await muscleRecoveryStateRepository.AddAsync(state, cancellationToken);
                }
                else
                {
                    state.RegisterSession(completedAtUtc, intensity, movementFamilies, exercise.Sets);
                    muscleRecoveryStateRepository.Update(state);
                }

                updatedMuscleGroups.Add(muscleGroup);
            }
        }

        // R4 (analytics): 1 log agregado por conclusao de quest (nao por grupo muscular),
        // mesmo padrao de "exercise_relationship_imported" - so dispara quando algo foi de fato atualizado.
        if (updatedMuscleGroups.Count > 0)
        {
            logger.LogInformation(
                "muscle_recovery_state_updated questId={QuestId} userId={UserId} muscleGroupsUpdated={MuscleGroupsUpdated}",
                quest.Id, quest.UserId, updatedMuscleGroups.Count);
        }
    }

    /// US-230: consome o efeito ativo de Poção de Foco/Grande (se houver) e
    /// retorna o bônus de XP a creditar sobre o total do treino que fechou.
    private async Task<long> TryConsumeXpBoostAsync(
        Guid userId, long totalXpEarned, CancellationToken cancellationToken)
    {
        var activeBoosts = await itemActiveEffectRepository.GetActiveByUserAndTypeAsync(
            userId, ItemEffectTypes.XpBoost, cancellationToken);

        var boost = activeBoosts
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.XpBoostMultiplier)
            .FirstOrDefault();

        if (boost is null || boost.XpBoostMultiplier is not { } multiplier)
            return 0;

        boost.Consume(dateTimeService.UtcNow);
        itemActiveEffectRepository.Update(boost);

        return (long)Math.Round(totalXpEarned * multiplier, MidpointRounding.AwayFromZero);
    }

    /// US-230: consome o efeito ativo do Amuleto de Retorno (se houver e ainda
    /// válido para hoje) e retorna quantos dias de streak restaurar, ou null
    /// se não houver recuperação elegível.
    private async Task<int?> TryConsumeStreakRecoveryAsync(
        Guid userId, DateTime completedAtUtc, CancellationToken cancellationToken)
    {
        var activeRecoveries = await itemActiveEffectRepository.GetActiveByUserAndTypeAsync(
            userId, ItemEffectTypes.StreakRecovery, cancellationToken);

        var recovery = activeRecoveries.FirstOrDefault(e => e.IsActive);
        if (recovery is null || recovery.StreakDaysToRestore is not { } days)
            return null;

        recovery.Consume(completedAtUtc);
        itemActiveEffectRepository.Update(recovery);

        return days;
    }

    private static QuestVisibleAttributeImpactsDto SumExerciseLevelUps(
        IReadOnlyList<QuestExercise> exercises) =>
        exercises.Aggregate(
            new QuestVisibleAttributeImpactsDto(0, 0, 0, 0, 0),
            (acc, e) => new QuestVisibleAttributeImpactsDto(
                acc.Strength + e.StrengthLevelUps,
                acc.Agility + e.AgilityLevelUps,
                acc.Endurance + e.EnduranceLevelUps,
                acc.Vitality + e.VitalityLevelUps,
                acc.Focus + e.FocusLevelUps));

    private static CompleteQuestResponse BuildResponse(Quest quest, QuestLog? questLog)
    {
        if (questLog is null)
        {
            // Defensivo: quest concluida sem log correspondente nao deveria ocorrer
            // (criados na mesma transacao), mas evita expor estado inconsistente.
            return new CompleteQuestResponse(
                quest.Id,
                quest.Type,
                quest.Status,
                quest.XpAwarded,
                new QuestAttributeXpDto(0, 0, 0, 0, 0, 0),
                new QuestVisibleAttributeImpactsDto(0, 0, 0, 0, 0),
                [],
                quest.CompletedAtUtc ?? quest.UpdatedAtUtc ?? quest.CreatedAtUtc);
        }

        return new CompleteQuestResponse(
            quest.Id,
            quest.Type,
            quest.Status,
            questLog.XpEarned,
            new QuestAttributeXpDto(
                questLog.StrengthXpEarned,
                questLog.AgilityXpEarned,
                questLog.EnduranceXpEarned,
                questLog.VitalityXpEarned,
                questLog.FocusXpEarned,
                questLog.WisdomXpEarned),
            new QuestVisibleAttributeImpactsDto(
                questLog.StrengthPointsGranted,
                questLog.AgilityPointsGranted,
                questLog.EndurancePointsGranted,
                questLog.VitalityPointsGranted,
                questLog.FocusPointsGranted),
            questLog.ItemsEarned,
            questLog.CompletedAtUtc);
    }
}
