namespace Awaken.Contracts.Admin.Routines;

/// <summary>
/// US-221: rotina recorrente registrada no Hangfire, com saúde derivada (RN-001, RN-002).
/// </summary>
public record RecurringRoutineResponse(
    string Id,
    string Cron,
    string Queue,
    string Status,
    bool IsDelayed,
    string? LastJobId,
    string? LastJobState,
    DateTime? LastExecutionUtc,
    DateTime? NextExecutionUtc,
    double? LastDurationSeconds,
    string ItemsProcessedLastBatch,
    string? LastErrorMessage);
