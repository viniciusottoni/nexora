using System.Text.Json;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;

public class ValidateTrainingTypeChangeQueryHandler(
    IQuestRepository questRepository,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IWorkoutGeneratorService workoutGeneratorService,
    ICurrentUserService currentUserService)
    : IRequestHandler<ValidateTrainingTypeChangeQuery, ValidateTrainingTypeChangeResponse>
{
    public async Task<ValidateTrainingTypeChangeResponse> Handle(
        ValidateTrainingTypeChangeQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.UserId != userId)
            throw new UnauthorizedException("QUEST_NOT_OWNED", "Quest nao pertence ao usuario atual.");

        // RN-001: validacao so e permitida antes de iniciar.
        if (quest.Status is "in_progress" or "completed")
            throw new ConflictException("QUEST_ALREADY_STARTED",
                "A quest ja foi iniciada. Nao e possivel alterar o tipo de treino.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        // RN-002/RN-003/RN-004: gera (sem persistir) o treino compativel com o tipo escolhido.
        var workoutJson = await BuildWorkoutJsonAsync(request, userId, user.PreferredLanguage, cancellationToken);

        // RN-005: recalcula duracao e XP a partir do treino gerado (mesma formula do QuestResponseMapper).
        var estimatedDurationMinutes = ParseDurationMinutes(workoutJson);
        var estimatedXp = (long)Math.Round(estimatedDurationMinutes * 4.0);

        return new ValidateTrainingTypeChangeResponse(
            Valid: true,
            EstimatedXp: estimatedXp,
            EstimatedDurationMinutes: estimatedDurationMinutes);
    }

    private async Task<string> BuildWorkoutJsonAsync(
        ValidateTrainingTypeChangeQuery request, Guid userId, string language, CancellationToken cancellationToken)
    {
        switch (request.TrainingType)
        {
            case "personalized_individual":
                var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserProfile", userId);
                var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);
                var fitnessProfileJson = FitnessProfileSnapshot.Build(profile, progression);
                // US-241: não passa userProfile/hunterProgression aqui de propósito - este é um
                // endpoint de VALIDAÇÃO/preview (não persiste nada), e WeeklyProgressionReviewer
                // tem efeito colateral de gravar estado; rodar aqui avançaria o mesociclo sem
                // uma geração real ter ocorrido.
                var result = await workoutGeneratorService.GenerateWorkoutJsonAsync(
                    userId, language, fitnessProfileJson, cancellationToken: cancellationToken);
                return result.WorkoutJson;

            case "regeneration":
                return TrainingTypeTemplates.RegenerationWorkoutJson(language);

            case "program":
                return request.ProgramId switch
                {
                    "saitama_path" => TrainingTypeTemplates.SaitamaPathWorkoutJson(language),
                    "perfect_2" => TrainingTypeTemplates.Perfect2WorkoutJson(language),
                    _ => throw new ConflictException("INVALID_PROGRAM_ID", $"Programa '{request.ProgramId}' nao reconhecido.")
                };

            default:
                throw new ConflictException("INVALID_TRAINING_TYPE",
                    $"Tipo de treino '{request.TrainingType}' nao reconhecido.");
        }
    }

    private static int ParseDurationMinutes(string? workoutJson)
    {
        if (string.IsNullOrWhiteSpace(workoutJson)) return 0;
        using var doc = JsonDocument.Parse(workoutJson);
        return doc.RootElement.TryGetProperty("durationMinutes", out var prop)
               && prop.TryGetInt32(out var minutes)
            ? minutes
            : 0;
    }
}
