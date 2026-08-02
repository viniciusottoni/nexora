using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Routines;
using Awaken.Shared.Admin;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using RecurringJobDto = Hangfire.Storage.RecurringJobDto;

namespace Awaken.Infrastructure.Jobs;

/// <summary>
/// US-221: introspecção somente leitura do Hangfire para o admin site — workers, rotinas
/// recorrentes, filas e execuções recentes.
///
/// RN-001: rotina atrasada (NextExecution no passado) aparece em destaque (Attention/Critical
///         conforme o tamanho do atraso).
/// RN-002: última execução em estado "Failed" torna a rotina Critical.
/// RN-003: zero servidores ativos (Servers().Count == 0, ou nenhum com heartbeat recente) deixa
///         toda a área de workers Critical.
/// RN-004: não existe, neste MVP, uma tabela/entidade dedicada a histórico de atualização
///         operacional controlada — retorna lista vazia com OperationalUpdatesAvailable = false
///         em vez de inventar dado. Fora desta US: executar atualização operacional manual.
/// RN-005: somente leitura — nenhum método aqui dispara, cancela ou reagenda jobs.
/// </summary>
public class JobMonitoringService(IDateTimeService dateTimeService) : IJobMonitoringService
{
    private const int RecentExecutionsLimit = 20;
    private const int FailedJobsSampleSize = 20;
    private const int SucceededJobsSampleSize = 20;

    private static readonly TimeSpan HeartbeatOnlineThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RoutineAttentionThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RoutineCriticalThreshold = TimeSpan.FromHours(2);

    public Task<RoutinesOverviewResponse> GetRoutinesOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = dateTimeService.UtcNow;
        var monitoringApi = JobStorage.Current.GetMonitoringApi();

        var workers = BuildWorkers(monitoringApi, now);
        var workersStatus = workers.Count == 0
            ? DomainHealthStatus.Critical // RN-003
            : (workers.Any(w => w.IsOnline) ? DomainHealthStatus.Healthy : DomainHealthStatus.Critical);

        var routines = BuildRoutines(now);
        var routinesStatus = DomainHealthStatus.Aggregate(routines.Select(r => r.Status));

        var queues = BuildQueues(monitoringApi);
        var queuesStatus = DomainHealthStatus.Aggregate(queues.Select(q => q.Status));

        var recentExecutions = BuildRecentExecutions(monitoringApi);

        return Task.FromResult(new RoutinesOverviewResponse(
            workersStatus,
            workers,
            routinesStatus,
            routines,
            queuesStatus,
            queues,
            recentExecutions,
            OperationalUpdatesAvailable: false, // RN-004: sem fonte real neste MVP.
            OperationalUpdates: [],
            now));
    }

    // ── Workers (RN-003) ─────────────────────────────────────────────────────

    private static List<WorkerStatusResponse> BuildWorkers(IMonitoringApi monitoringApi, DateTime now)
    {
        var servers = monitoringApi.Servers();

        return servers
            .Select(s =>
            {
                var isOnline = s.Heartbeat.HasValue && now - s.Heartbeat.Value <= HeartbeatOnlineThreshold;
                return new WorkerStatusResponse(
                    s.Name,
                    isOnline,
                    s.WorkersCount,
                    s.Queues?.ToList() ?? [],
                    DateTime.SpecifyKind(s.StartedAt, DateTimeKind.Utc),
                    s.Heartbeat.HasValue ? DateTime.SpecifyKind(s.Heartbeat.Value, DateTimeKind.Utc) : null);
            })
            .OrderByDescending(w => w.IsOnline)
            .ThenBy(w => w.Name)
            .ToList();
    }

    // ── Rotinas recorrentes (RN-001, RN-002) ─────────────────────────────────

    private List<RecurringRoutineResponse> BuildRoutines(DateTime now)
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        return recurringJobs
            .Select(job => BuildRoutine(job, now))
            .OrderBy(r => r.Id)
            .ToList();
    }

    private RecurringRoutineResponse BuildRoutine(RecurringJobDto job, DateTime now)
    {
        var lastExecution = job.LastExecution.HasValue
            ? DateTime.SpecifyKind(job.LastExecution.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var nextExecution = job.NextExecution.HasValue
            ? DateTime.SpecifyKind(job.NextExecution.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var isFailed = string.Equals(job.LastJobState, "Failed", StringComparison.OrdinalIgnoreCase);

        var overdueBy = nextExecution.HasValue && nextExecution.Value < now
            ? now - nextExecution.Value
            : TimeSpan.Zero;
        var isDelayed = overdueBy > TimeSpan.Zero;

        string status;
        if (isFailed)
        {
            status = DomainHealthStatus.Critical; // RN-002
        }
        else if (overdueBy >= RoutineCriticalThreshold)
        {
            status = DomainHealthStatus.Critical; // RN-001: atraso severo
        }
        else if (overdueBy >= RoutineAttentionThreshold)
        {
            status = DomainHealthStatus.Attention; // RN-001: atraso moderado
        }
        else if (!lastExecution.HasValue && !nextExecution.HasValue)
        {
            status = DomainHealthStatus.NoData;
        }
        else
        {
            status = DomainHealthStatus.Healthy;
        }

        return new RecurringRoutineResponse(
            job.Id,
            job.Cron,
            job.Queue ?? "default",
            status,
            isDelayed,
            job.LastJobId,
            job.LastJobState,
            lastExecution,
            nextExecution,
            LastDurationSeconds: null, // Hangfire não expõe duração agregada via RecurringJobDto.
            ItemsProcessedLastBatch: "Sem dados", // MVP: jobs não retornam volume processado estruturado.
            job.Error);
    }

    // ── Filas (RN-001 — fila acumulada) ──────────────────────────────────────

    private static List<QueueStatusResponse> BuildQueues(IMonitoringApi monitoringApi)
    {
        var queues = monitoringApi.Queues();

        return queues
            .Select(q =>
            {
                var fetched = q.Fetched ?? 0;
                var status = q.Length switch
                {
                    >= 500 => DomainHealthStatus.Critical,
                    >= 100 => DomainHealthStatus.Attention,
                    _ => DomainHealthStatus.Healthy,
                };

                return new QueueStatusResponse(q.Name, q.Length, fetched, status);
            })
            .OrderBy(q => q.QueueName)
            .ToList();
    }

    // ── Execuções recentes (sucesso + falha) ─────────────────────────────────

    private static List<RecentExecutionResponse> BuildRecentExecutions(IMonitoringApi monitoringApi)
    {
        var executions = new List<RecentExecutionResponse>();

        var failedJobs = monitoringApi.FailedJobs(0, FailedJobsSampleSize);
        foreach (var pair in failedJobs)
        {
            var dto = pair.Value;
            if (dto is null) continue;

            executions.Add(new RecentExecutionResponse(
                pair.Key,
                ResolveJobName(dto.Job),
                "failed",
                dto.FailedAt.HasValue ? DateTime.SpecifyKind(dto.FailedAt.Value, DateTimeKind.Utc) : default,
                DurationSeconds: null,
                dto.ExceptionMessage));
        }

        var succeededJobs = monitoringApi.SucceededJobs(0, SucceededJobsSampleSize);
        foreach (var pair in succeededJobs)
        {
            var dto = pair.Value;
            if (dto is null) continue;

            executions.Add(new RecentExecutionResponse(
                pair.Key,
                ResolveJobName(dto.Job),
                "succeeded",
                dto.SucceededAt.HasValue ? DateTime.SpecifyKind(dto.SucceededAt.Value, DateTimeKind.Utc) : default,
                dto.TotalDuration.HasValue ? dto.TotalDuration.Value / 1000.0 : null,
                ErrorMessage: null));
        }

        return executions
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(RecentExecutionsLimit)
            .ToList();
    }

    private static string ResolveJobName(Hangfire.Common.Job? job)
    {
        if (job is null) return "Desconhecido";

        var typeName = job.Type.Name;
        var methodName = job.Method.Name;
        return $"{typeName}.{methodName}";
    }
}
