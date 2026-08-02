namespace Awaken.Contracts.Admin.Routines;

/// <summary>
/// US-221: visão operacional completa de rotinas recorrentes, workers, filas e atualizações
/// operacionais controladas. Somente leitura (RN-005) — nenhum campo desta resposta representa
/// uma ação disponível para o Admin executar por esta tela.
/// </summary>
public record RoutinesOverviewResponse(
    string WorkersStatus,
    IReadOnlyList<WorkerStatusResponse> Workers,
    string RoutinesStatus,
    IReadOnlyList<RecurringRoutineResponse> Routines,
    string QueuesStatus,
    IReadOnlyList<QueueStatusResponse> Queues,
    IReadOnlyList<RecentExecutionResponse> RecentExecutions,
    bool OperationalUpdatesAvailable,
    IReadOnlyList<OperationalUpdateResponse> OperationalUpdates,
    DateTime GeneratedAtUtc);
