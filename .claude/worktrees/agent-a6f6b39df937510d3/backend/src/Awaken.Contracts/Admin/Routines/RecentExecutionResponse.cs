namespace Awaken.Contracts.Admin.Routines;

/// <summary>US-221: item do histórico recente de execuções (sucesso ou falha) para a timeline da tela.</summary>
public record RecentExecutionResponse(
    string JobId,
    string JobName,
    string Outcome,
    DateTime OccurredAtUtc,
    double? DurationSeconds,
    string? ErrorMessage);
