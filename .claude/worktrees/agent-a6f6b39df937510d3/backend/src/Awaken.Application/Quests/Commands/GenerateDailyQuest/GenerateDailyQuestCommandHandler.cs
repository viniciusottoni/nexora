using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Services;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Quests.Commands.GenerateDailyQuest;

public class GenerateDailyQuestCommandHandler(
    IQuestRepository questRepository,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IWorkoutGeneratorService workoutGeneratorService,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUserDateService userDateService,
    IDailyQuestPenaltyService dailyQuestPenaltyService,
    IUnitOfWork unitOfWork,
    ILogger<GenerateDailyQuestCommandHandler> logger) : IRequestHandler<GenerateDailyQuestCommand, QuestResponse>
{
    private const string DailyType = "daily";

    public async Task<QuestResponse> Handle(
        GenerateDailyQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var questDateUtc = DateTime.SpecifyKind(
            userDateService.TodayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        await dailyQuestPenaltyService.ApplyForUserBeforeDateAsync(userId, questDateUtc, cancellationToken);

        // RN-002/US-047: no máximo uma quest diária por usuário por dia (idempotência).
        var existing = await questRepository.GetByUserIdAndDateAsync(
            userId, DailyType, questDateUtc, cancellationToken);
        if (existing is not null)
        {
            return QuestResponseMapper.ToResponse(existing);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("UserProfile", userId);

        // US-151/152: atributos do Hunter para targetAttributeScore; null é tolerado pelo scoring.
        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);

        var fitnessProfileJson = FitnessProfileSnapshot.Build(profile, progression);

        var idempotencyKey = $"{userId:N}_{questDateUtc:yyyyMMdd}_{DailyType}";

        var quest = Quest.Create(userId, questDateUtc, user.PreferredLanguage, idempotencyKey);

        var utcNow = dateTimeService.UtcNow;
        var workoutResult = await workoutGeneratorService.GenerateWorkoutJsonAsync(
            userId, user.PreferredLanguage, fitnessProfileJson,
            userProfile: profile, hunterProgression: progression, cancellationToken: cancellationToken);
        quest.AssignWorkout(workoutResult.WorkoutJson, utcNow, workoutResult.IsPersonalized);

        // US-238/US-240: registra o dia do programa resolvido pela rotação e o
        // blueprint usado para compor o conjunto elegível, quando o programa tem
        // split clássico configurado (RN-009: nulo para perfect_2/system).
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

        // US-242: registra o orçamento de tempo determinístico usado nesta geração.
        if (workoutResult.EstimatedDurationSeconds is not null
            && workoutResult.TimeBudgetSeconds is not null
            && workoutResult.TimeAdjustmentApplied is not null
            && workoutResult.WorkoutTimeModelVersion is not null)
        {
            quest.AssignTimeBudget(
                workoutResult.EstimatedDurationSeconds.Value,
                workoutResult.TimeBudgetSeconds.Value,
                workoutResult.TimeAdjustmentApplied,
                workoutResult.WorkoutTimeModelVersion,
                utcNow);
        }

        // US-241: registra o plano de progressão semanal usado nesta geração.
        if (workoutResult.WeeklyProgressionPlanJson is not null)
        {
            quest.AssignWeeklyProgressionPlan(workoutResult.WeeklyProgressionPlanJson, utcNow);
        }

        // US-049: registra motivo/metodo e snapshot do perfil/filtros considerados.
        quest.RegisterGenerationAudit(
            generationReason: workoutResult.IsPersonalized ? "rules" : "fallback",
            generationMethod: workoutResult.GenerationMethod,
            profileSnapshotJson: fitnessProfileJson,
            appliedFiltersJson: workoutResult.AppliedFiltersJson,
            utcNow: utcNow);

        await questRepository.AddAsync(quest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "daily_quest_generated questId={QuestId} userId={UserId} questType={QuestType} isPersonalized={IsPersonalized}",
            quest.Id, userId, quest.Type, workoutResult.IsPersonalized);

        return QuestResponseMapper.ToResponse(quest);
    }
}
