using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Onboarding;
using Awaken.Contracts.Progression;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Progression;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUserRepository userRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<CompleteOnboardingCommandHandler> logger) : IRequestHandler<CompleteOnboardingCommand, CompleteOnboardingResponse>
{
    public async Task<CompleteOnboardingResponse> Handle(
        CompleteOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            profile = UserProfile.Create(
                userId: userId,
                goal: request.Goal,
                experienceLevel: request.ExperienceLevel,
                age: request.Age,
                heightCm: request.HeightCm,
                weightKg: request.WeightKg,
                biologicalSex: request.BiologicalSex,
                trainingDuration: request.TrainingDuration,
                availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
                bodyType: request.BodyType,
                physicalLimitations: request.PhysicalLimitations,
                physicalPains: request.PhysicalPains);
            await userProfileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.ApplyPatch(
                age: request.Age,
                heightCm: request.HeightCm,
                weightKg: request.WeightKg,
                biologicalSex: request.BiologicalSex,
                trainingDuration: request.TrainingDuration,
                trainingLocation: null,
                equipmentAvailable: null,
                availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
                availableDaysPerWeek: null,
                bodyType: request.BodyType,
                physicalLimitations: request.PhysicalLimitations,
                physicalPains: request.PhysicalPains,
                trainingPreferences: null,
                utcNow: utcNow,
                goal: request.Goal,
                experienceLevel: request.ExperienceLevel);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var wasAlreadyCompleted = user!.IsOnboardingComplete;
        if (!wasAlreadyCompleted)
            user.CompleteOnboarding(utcNow);

        // RN-005/Idempotência: o Rank/RankScore iniciais são calculados uma única vez,
        // novas conclusões de onboarding não recalculam progresso já existente.
        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);
        var rankCapApplied = false;

        if (progression is null)
        {
            var initialProgression = OnboardingInitialProgressionCalculator.Calculate(
                request.ExperienceLevel,
                request.TrainingDuration);

            progression = HunterProgression.CreateFromOnboarding(
                userId: userId,
                strength: initialProgression.Attributes.Strength,
                agility: initialProgression.Attributes.Agility,
                endurance: initialProgression.Attributes.Endurance,
                vitality: initialProgression.Attributes.Vitality,
                focus: initialProgression.Attributes.Focus,
                wisdom: initialProgression.Attributes.Wisdom);
            await hunterProgressionRepository.AddAsync(progression, cancellationToken);
            rankCapApplied = initialProgression.RankCapApplied;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!wasAlreadyCompleted)
        {
            logger.LogInformation(
                "onboarding_completed userId={UserId} rank={Rank} level={Level} rankScore={RankScore} rankCapApplied={RankCapApplied}",
                userId, progression.Rank, progression.Level, progression.RankScore, rankCapApplied);
        }

        return new CompleteOnboardingResponse(
            OnboardingCompleted: true,
            NextRoute: "daily_quest",
            Rank: progression.Rank,
            RankScore: progression.RankScore,
            Level: progression.Level,
            Attributes: new AttributesDto(
                progression.Strength,
                progression.Endurance,
                progression.Agility,
                progression.Vitality,
                progression.Focus,
                progression.Wisdom),
            RankCapApplied: rankCapApplied);
    }
}
