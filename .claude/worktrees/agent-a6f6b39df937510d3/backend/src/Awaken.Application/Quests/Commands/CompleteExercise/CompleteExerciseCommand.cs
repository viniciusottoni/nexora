using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.CompleteExercise;

/// US-064/US-065: conclui um exercício da quest concedendo XP calculado proporcionalmente.
public record CompleteExerciseCommand(
    Guid QuestId,
    Guid QuestExerciseId,
    int SetsCompleted,
    bool StrongPainReported = false) : IRequest<CompleteExerciseResponse>;
