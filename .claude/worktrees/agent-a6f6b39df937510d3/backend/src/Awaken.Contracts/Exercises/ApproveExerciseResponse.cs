namespace Awaken.Contracts.Exercises;

/// <summary>US-149 (R3.3) — resposta do approve, no formato conceitual sugerido pela US-149 §17.</summary>
public record ApproveExerciseResponse(Guid Id, string Status, bool IsApprovedForWorkoutGeneration);
