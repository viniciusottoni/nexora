namespace Awaken.Contracts.Admin.Routines;

/// <summary>
/// US-221: status de um worker (servidor Hangfire) ativo conhecido pelo storage.
/// RN-003: ausência de qualquer servidor ativo deixa a área inteira crítica — ver
/// RoutinesOverviewResponse.WorkersStatus.
/// </summary>
public record WorkerStatusResponse(
    string Name,
    bool IsOnline,
    int WorkersCount,
    IReadOnlyList<string> Queues,
    DateTime? StartedAtUtc,
    DateTime? LastHeartbeatUtc);
