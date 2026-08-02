using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestRewardSummary;

/// US-063: retorna o resumo de recompensa de uma quest concluida, baseado no
/// QuestLog (RN-001/RN-005: backend e' a autoridade, frontend nao recalcula).
public class GetQuestRewardSummaryQueryHandler(
    IQuestRepository questRepository,
    IQuestLogRepository questLogRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetQuestRewardSummaryQuery, QuestRewardSummaryResponse>
{
    public async Task<QuestRewardSummaryResponse> Handle(
        GetQuestRewardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdWithExercisesAsync(request.QuestId, cancellationToken);

        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        var questLog = await questLogRepository.GetByQuestIdAsync(quest.Id, cancellationToken)
            ?? throw new ConflictException("QUEST_NOT_COMPLETED", "Quest ainda nao foi concluida.");

        var progression = await hunterProgressionRepository.GetByUserIdAsync(quest.UserId, cancellationToken);

        var source = quest.Type switch
        {
            "daily" => "daily",
            "dungeon" => "dungeon",
            "raid" => "raid",
            _ => "quest",
        };

        // US-136: nivel novo vem do backend a partir da progressao atual,
        // sem recalcular no app. Repetimos a entrada por level-up real.
        var attributeLevelUpDetails = BuildAttributeLevelUpDetails(
            quest, questLog, progression, source);

        // US-138: item reward vem com metadados de backend para analytics.
        var itemRewards = questLog.ItemsEarned
            .Select(itemId => new QuestItemRewardDto(
                itemId,
                GetItemRarity(itemId),
                source))
            .ToList();

        return new QuestRewardSummaryResponse(
            quest.Id,
            quest.Type,
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
            progression?.CurrentStreakDays ?? 0,
            questLog.ItemsEarned,
            attributeLevelUpDetails.Select(levelUp => levelUp.Attribute).ToList(),
            attributeLevelUpDetails,
            itemRewards);
    }

    private static List<QuestAttributeLevelUpDto> BuildAttributeLevelUpDetails(
        Quest quest,
        QuestLog questLog,
        HunterProgression? progression,
        string source)
    {
        if (progression is null)
            return [];

        var details = new List<QuestAttributeLevelUpDto>(6);

        AppendLevelUps(
            details,
            "strength",
            GetLevelUps(quest.Exercises, exercise => exercise.StrengthLevelUps, questLog.StrengthPointsGranted),
            progression.Strength,
            source);
        AppendLevelUps(
            details,
            "agility",
            GetLevelUps(quest.Exercises, exercise => exercise.AgilityLevelUps, questLog.AgilityPointsGranted),
            progression.Agility,
            source);
        AppendLevelUps(
            details,
            "endurance",
            GetLevelUps(quest.Exercises, exercise => exercise.EnduranceLevelUps, questLog.EndurancePointsGranted),
            progression.Endurance,
            source);
        AppendLevelUps(
            details,
            "vitality",
            GetLevelUps(quest.Exercises, exercise => exercise.VitalityLevelUps, questLog.VitalityPointsGranted),
            progression.Vitality,
            source);
        AppendLevelUps(
            details,
            "focus",
            GetLevelUps(quest.Exercises, exercise => exercise.FocusLevelUps, questLog.FocusPointsGranted),
            progression.Focus,
            source);

        return details;
    }

    private static int GetLevelUps(
        IEnumerable<QuestExercise> exercises,
        Func<QuestExercise, int> selector,
        int fallback)
    {
        var exerciseLevelUps = exercises.Sum(selector);
        return Math.Max(exerciseLevelUps, fallback);
    }

    private static void AppendLevelUps(
        List<QuestAttributeLevelUpDto> details,
        string attribute,
        int levelUps,
        int currentLevel,
        string source)
    {
        if (levelUps <= 0)
            return;

        var firstNewLevel = Math.Max(1, currentLevel - levelUps + 1);
        for (var newLevel = firstNewLevel; newLevel <= currentLevel; newLevel++)
        {
            details.Add(new QuestAttributeLevelUpDto(attribute, newLevel, source));
        }
    }

    private static string GetItemRarity(string itemId) => itemId switch
    {
        "pedra_dungeon" => "consumable",
        "reforja_scroll" => "consumable",
        _ => "common",
    };
}
