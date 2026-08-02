using Awaken.Application.Admin.Routines.Queries.GetRoutinesOverview;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Routines;
using Awaken.Shared.Admin;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Routines;

/// <summary>
/// US-221 — testes do handler de visão operacional de rotinas/workers/filas.
///
/// O handler é um repasse fino para IJobMonitoringService; os cenários abaixo garantem que o
/// resultado do serviço (mockado) chega intacto ao caller e cobrem os casos exigidos pelo QA da
/// US-221: worker ativo, worker parado, rotina bem-sucedida, rotina com falha, rotina atrasada e
/// fila acumulada.
/// </summary>
public class GetRoutinesOverviewQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IJobMonitoringService> _jobMonitoringService = new();

    private GetRoutinesOverviewQueryHandler CreateHandler() => new(_jobMonitoringService.Object);

    [Fact]
    public async Task Handle_WhenWorkerActive_ReturnsHealthyWorkersStatus()
    {
        var worker = new WorkerStatusResponse("worker-1", IsOnline: true, 4, ["default"], UtcNow.AddHours(-1), UtcNow.AddSeconds(-10));
        var response = BuildResponse(workers: [worker], workersStatus: DomainHealthStatus.Healthy);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.WorkersStatus.Should().Be(DomainHealthStatus.Healthy);
        result.Workers.Should().ContainSingle(w => w.IsOnline);
    }

    [Fact]
    public async Task Handle_WhenWorkerStopped_ReturnsCriticalWorkersStatus()
    {
        // RN-003: nenhum worker ativo deixa a área toda crítica.
        var worker = new WorkerStatusResponse("worker-1", IsOnline: false, 0, ["default"], UtcNow.AddDays(-1), UtcNow.AddHours(-3));
        var response = BuildResponse(workers: [worker], workersStatus: DomainHealthStatus.Critical);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.WorkersStatus.Should().Be(DomainHealthStatus.Critical);
        result.Workers.Should().OnlyContain(w => !w.IsOnline);
    }

    [Fact]
    public async Task Handle_WhenRoutineSucceeded_ReturnsHealthyRoutine()
    {
        var routine = new RecurringRoutineResponse(
            "daily-quest-reminder", "0 8 * * *", "notifications", DomainHealthStatus.Healthy,
            IsDelayed: false, "job-1", "Succeeded", UtcNow.AddHours(-4), UtcNow.AddHours(20),
            LastDurationSeconds: 1.2, ItemsProcessedLastBatch: "Sem dados", LastErrorMessage: null);
        var response = BuildResponse(routines: [routine], routinesStatus: DomainHealthStatus.Healthy);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.Routines.Should().ContainSingle(r => r.Status == DomainHealthStatus.Healthy && r.LastJobState == "Succeeded");
    }

    [Fact]
    public async Task Handle_WhenRoutineFailed_ReturnsCriticalRoutine()
    {
        // RN-002: falha recorrente gera status crítico.
        var routine = new RecurringRoutineResponse(
            "missed-daily-quest-notification", "10 0 * * *", "notifications", DomainHealthStatus.Critical,
            IsDelayed: false, "job-2", "Failed", UtcNow.AddHours(-1), UtcNow.AddHours(23),
            LastDurationSeconds: null, ItemsProcessedLastBatch: "Sem dados", LastErrorMessage: "Timeout ao enviar push.");
        var response = BuildResponse(routines: [routine], routinesStatus: DomainHealthStatus.Critical);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.Routines.Should().ContainSingle(r =>
            r.Status == DomainHealthStatus.Critical && r.LastErrorMessage == "Timeout ao enviar push.");
    }

    [Fact]
    public async Task Handle_WhenRoutineDelayed_ReturnsAttentionOrCriticalAndIsDelayedTrue()
    {
        // RN-001: rotina atrasada deve aparecer em destaque.
        var routine = new RecurringRoutineResponse(
            "streak-risk-alert", "0 20 * * *", "notifications", DomainHealthStatus.Attention,
            IsDelayed: true, "job-3", "Succeeded", UtcNow.AddDays(-1), UtcNow.AddMinutes(-30),
            LastDurationSeconds: 0.8, ItemsProcessedLastBatch: "Sem dados", LastErrorMessage: null);
        var response = BuildResponse(routines: [routine], routinesStatus: DomainHealthStatus.Attention);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.Routines.Should().ContainSingle(r => r.IsDelayed);
        result.Routines.Single().Status.Should().BeOneOf(DomainHealthStatus.Attention, DomainHealthStatus.Critical);
    }

    [Fact]
    public async Task Handle_WhenQueueAccumulated_ReturnsCriticalQueueStatus()
    {
        // Fila acumulada deve ficar visível e crítica conforme o tamanho.
        var queue = new QueueStatusResponse("notifications", EnqueuedCount: 850, FetchedCount: 3, DomainHealthStatus.Critical);
        var response = BuildResponse(queues: [queue], queuesStatus: DomainHealthStatus.Critical);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.QueuesStatus.Should().Be(DomainHealthStatus.Critical);
        result.Queues.Should().ContainSingle(q => q.EnqueuedCount == 850);
    }

    [Fact]
    public async Task Handle_WhenNoOperationalUpdateSourceExists_ReturnsEmptyListWithFlagFalse()
    {
        // RN-004: sem tabela dedicada de atualização operacional, retorna vazio + flag honesta.
        var response = BuildResponse(operationalUpdatesAvailable: false, operationalUpdates: []);
        _jobMonitoringService.Setup(s => s.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetRoutinesOverviewQuery(), CancellationToken.None);

        result.OperationalUpdatesAvailable.Should().BeFalse();
        result.OperationalUpdates.Should().BeEmpty();
    }

    private static RoutinesOverviewResponse BuildResponse(
        IReadOnlyList<WorkerStatusResponse>? workers = null,
        string workersStatus = DomainHealthStatus.NoData,
        IReadOnlyList<RecurringRoutineResponse>? routines = null,
        string routinesStatus = DomainHealthStatus.NoData,
        IReadOnlyList<QueueStatusResponse>? queues = null,
        string queuesStatus = DomainHealthStatus.NoData,
        IReadOnlyList<RecentExecutionResponse>? recentExecutions = null,
        bool operationalUpdatesAvailable = false,
        IReadOnlyList<OperationalUpdateResponse>? operationalUpdates = null) =>
        new(
            workersStatus,
            workers ?? [],
            routinesStatus,
            routines ?? [],
            queuesStatus,
            queues ?? [],
            recentExecutions ?? [],
            operationalUpdatesAvailable,
            operationalUpdates ?? [],
            UtcNow);
}
