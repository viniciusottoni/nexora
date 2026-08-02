using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Commands.ChangeTrainingType;

public class ChangeTrainingTypeCommandHandler(
    IQuestRepository questRepository,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IWorkoutGeneratorService workoutGeneratorService,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeTrainingTypeCommand, QuestPreviewResponse>
{
    public async Task<QuestPreviewResponse> Handle(
        ChangeTrainingTypeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.UserId != userId)
            throw new UnauthorizedException("QUEST_NOT_OWNED", "Quest nao pertence ao usuario atual.");

        // RN-001 (US-051): alteracao so e permitida antes de iniciar (status == "pending").
        if (quest.Status is "in_progress" or "completed")
            throw new ConflictException("QUEST_ALREADY_STARTED",
                "A quest ja foi iniciada. Nao e possivel alterar o tipo de treino.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        string workoutJson;
        bool isPersonalized;

        switch (request.TrainingType)
        {
            case "personalized_individual":
                var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserProfile", userId);
                var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);
                var fitnessProfileJson = FitnessProfileSnapshot.Build(profile, progression);
                var result = await workoutGeneratorService.GenerateWorkoutJsonAsync(
                    userId, user.PreferredLanguage, fitnessProfileJson,
                    userProfile: profile, hunterProgression: progression, cancellationToken: cancellationToken);
                workoutJson = result.WorkoutJson;
                isPersonalized = result.IsPersonalized;
                break;

            case "regeneration":
                workoutJson = TrainingTypeTemplates.RegenerationWorkoutJson(user.PreferredLanguage);
                isPersonalized = false;
                break;

            case "program":
                workoutJson = request.ProgramId switch
                {
                    "saitama_path" => TrainingTypeTemplates.SaitamaPathWorkoutJson(user.PreferredLanguage),
                    "perfect_2" => TrainingTypeTemplates.Perfect2WorkoutJson(user.PreferredLanguage),
                    _ => throw new ConflictException("INVALID_PROGRAM_ID", $"Programa '{request.ProgramId}' nao reconhecido.")
                };
                isPersonalized = false;
                break;

            default:
                throw new ConflictException("INVALID_TRAINING_TYPE",
                    $"Tipo de treino '{request.TrainingType}' nao reconhecido.");
        }

        quest.ChangeTrainingType(request.TrainingType, request.ProgramId, workoutJson, isPersonalized, dateTimeService.UtcNow);
        questRepository.Update(quest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return QuestResponseMapper.ToPreviewResponse(quest);
    }
}
