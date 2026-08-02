using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestExecution;

/// US-057: consulta a execucao em andamento de uma quest, com exercicios ordenados.
public class GetQuestExecutionQueryHandler(
    IQuestRepository questRepository,
    IExerciseCatalogRepository exerciseCatalogRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetQuestExecutionQuery, QuestExecutionResponse>
{
    public async Task<QuestExecutionResponse> Handle(
        GetQuestExecutionQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdWithExercisesAsync(request.QuestId, cancellationToken);

        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        // RN-001/RN-006: quest em andamento ou pausada pode ser acompanhada
        // (US-059: tela de execucao exibe o botao de retomar quando pausada).
        if (quest.Status is not ("in_progress" or "paused"))
            throw new ConflictException("QUEST_NOT_IN_PROGRESS", "Quest nao esta em andamento.");

        // 9.1: quest sem exercicios deve impedir a execucao com erro controlado.
        if (quest.Exercises.Count == 0)
            throw new ConflictException("QUEST_HAS_NO_EXERCISES", "Quest nao possui exercicios.");

        var orderedExercises = quest.Exercises.OrderBy(e => e.Order).ToList();
        var exercises = new List<QuestExerciseDto>(orderedExercises.Count);
        foreach (var e in orderedExercises)
        {
            var projection = QuestExerciseRewardMapper.Project(e);
            // US-041: QuestExercise nao guarda instrucoes/dicas - resolve em tempo de
            // consulta via ExerciseCatalogProviderId, mesmo padrao usado na US-239
            // (CompleteQuestCommandHandler.UpdateMuscleRecoveryStateAsync) para grupos musculares.
            var (instructions, tips) = await ResolveInstructionsAndTipsAsync(
                e.ExerciseCatalogProviderId, cancellationToken);

            exercises.Add(new QuestExerciseDto(
                QuestExerciseId: e.Id,
                Order: e.Order,
                Status: e.Status,
                Name: e.Name,
                Sets: e.Sets,
                RepsMin: e.RepsMin,
                RepsMax: e.RepsMax,
                RestSeconds: e.RestSeconds,
                TargetRpe: e.TargetRpe,
                VideoUrl: e.VideoUrl,
                XpReward: e.XpReward,
                EffectiveDifficulty: projection.EffectiveDifficulty,
                AttributeImpacts: projection.VisibleImpacts,
                HiddenAttributeImpacts: projection.HiddenImpacts,
                CompletedAtUtc: e.CompletedAtUtc,
                ProviderExerciseId: e.ExerciseCatalogProviderId,
                Instructions: instructions,
                Tips: tips));
        }

        return new QuestExecutionResponse(
            QuestId: quest.Id,
            QuestType: quest.Type,
            Status: quest.Status,
            StartedAtUtc: quest.StartedAtUtc!.Value,
            AttributeXpPreview: QuestExerciseRewardMapper.Summarize(quest.Exercises),
            Exercises: exercises);
    }

    /// US-041: resolve InstructionsPtBr/TipsPtBr do ExerciseCatalog a partir do
    /// ExerciseCatalogProviderId gravado no QuestExercise. Exercicios de fallback
    /// (sem provider id) ou cujo catalogo nao for encontrado ficam com listas
    /// vazias, sem derrubar o fluxo de consulta (mesmo principio da US-239).
    private async Task<(IReadOnlyList<string> Instructions, IReadOnlyList<string> Tips)> ResolveInstructionsAndTipsAsync(
        string? exerciseCatalogProviderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(exerciseCatalogProviderId))
            return ([], []);

        var catalog = await exerciseCatalogRepository.GetByProviderExerciseIdAsync(
            exerciseCatalogProviderId, cancellationToken);

        return catalog is null ? ([], []) : (catalog.InstructionsPtBr, catalog.TipsPtBr);
    }
}
