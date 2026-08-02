using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;

namespace Awaken.Application.Quests.Common;

/// <summary>US-230: ver <see cref="IQuestRegenerationService"/>.</summary>
public class QuestRegenerationService(
    IQuestRepository questRepository,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IWorkoutGeneratorService workoutGeneratorService,
    IUserDateService userDateService,
    IDateTimeService dateTimeService) : IQuestRegenerationService
{
    private const string DailyType = "daily";

    public async Task<Quest> RegenerateAsync(Guid userId, bool viaReforgeScroll, CancellationToken cancellationToken)
    {
        var questDateUtc = DateTime.SpecifyKind(
            userDateService.TodayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var quest = await questRepository.GetByUserIdAndDateAsync(
            userId, DailyType, questDateUtc, cancellationToken)
            ?? throw new NotFoundException("Quest", userId);

        var utcNow = dateTimeService.UtcNow;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("UserProfile", userId);

        // RN-002/RN-004: mesma fonte de perfil/seguranca da geracao original, garantindo
        // que limitacoes e dores nunca sejam contornadas pela regeneracao.
        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);
        var fitnessProfileJson = FitnessProfileSnapshot.Build(profile, progression);

        var workoutResult = await workoutGeneratorService.GenerateWorkoutJsonAsync(
            userId, user.PreferredLanguage, fitnessProfileJson,
            userProfile: profile, hunterProgression: progression, cancellationToken: cancellationToken);

        quest.Regenerate(workoutResult.WorkoutJson, workoutResult.IsPersonalized, viaReforgeScroll, utcNow);

        // US-238/US-240: mesma auditoria de dia/blueprint da geração original -
        // a regeneração não pode deixar a rotação nem o registro de auditoria "para trás".
        if (workoutResult.ResolvedProgramKey is not null
            && workoutResult.ResolvedDayKey is not null
            && workoutResult.ResolvedDayIndex is not null
            && workoutResult.SplitMapVersion is not null)
        {
            quest.AssignResolvedProgramDay(
                workoutResult.ResolvedProgramKey,
                workoutResult.ResolvedDayKey,
                workoutResult.ResolvedDayIndex.Value,
                workoutResult.SplitMapVersion,
                utcNow);
        }

        if (workoutResult.DailyWorkoutBlueprintJson is not null)
        {
            quest.AssignDailyWorkoutBlueprint(workoutResult.DailyWorkoutBlueprintJson, utcNow);
        }

        quest.RegisterGenerationAudit(
            generationReason: "regeneration",
            generationMethod: viaReforgeScroll
                ? $"{workoutResult.GenerationMethod}+reforge_scroll"
                : workoutResult.GenerationMethod,
            profileSnapshotJson: fitnessProfileJson,
            appliedFiltersJson: workoutResult.AppliedFiltersJson,
            utcNow: utcNow);

        questRepository.Update(quest);
        return quest;
    }
}
