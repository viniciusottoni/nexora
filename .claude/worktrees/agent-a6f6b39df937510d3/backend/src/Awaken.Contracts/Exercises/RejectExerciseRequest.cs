namespace Awaken.Contracts.Exercises;

/// <summary>US-149 (R3.3) — corpo do <c>POST /api/admin/exercises/{id}/reject</c>. RN-005: motivo é obrigatório.</summary>
public record RejectExerciseRequest(string Reason);
