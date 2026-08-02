namespace Awaken.Contracts.Exercises;

/// <summary>US-149 (R3.3) — resposta do reject, com o motivo gravado ecoado para confirmação do cliente.</summary>
public record RejectExerciseResponse(Guid Id, string Status, string RejectionReason);
