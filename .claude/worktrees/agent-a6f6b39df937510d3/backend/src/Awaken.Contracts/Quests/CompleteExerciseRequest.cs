namespace Awaken.Contracts.Quests;

/// US-064/US-065: dados de conclusão do exercício enviados pelo cliente.
public record CompleteExerciseRequest(int SetsCompleted, bool StrongPainReported = false);
