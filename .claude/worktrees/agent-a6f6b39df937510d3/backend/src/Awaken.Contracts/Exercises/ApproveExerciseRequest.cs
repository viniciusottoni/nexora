namespace Awaken.Contracts.Exercises;

/// <summary>
/// US-149 (R3.3) — corpo do <c>POST /api/admin/exercises/{id}/approve</c>. Vazio hoje (o id vem da
/// rota e o revisor vem do usuário autenticado); existe como contrato próprio para dar espaço a campos
/// futuros (ex.: nota da curadoria) sem quebrar o endpoint.
/// </summary>
public record ApproveExerciseRequest;
