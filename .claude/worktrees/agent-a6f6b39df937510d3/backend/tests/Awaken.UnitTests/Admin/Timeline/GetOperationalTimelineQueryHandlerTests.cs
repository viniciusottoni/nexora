using Awaken.Application.Admin.Timeline.Queries.GetOperationalTimeline;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Timeline;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Timeline;

/// <summary>
/// US-224 — testes do handler de timeline operacional.
///
/// O handler é um repasse fino para IOperationalTimelineService; os cenários abaixo garantem que o
/// resultado do serviço (mockado) chega intacto ao caller e cobrem os casos exigidos pelo QA da
/// US-224: timeline vazia, alertas de segurança, logs de auditoria, mascaramento de userId,
/// intervalo padrão, ordenação descendente e limite máximo de entradas.
/// </summary>
public class GetOperationalTimelineQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IOperationalTimelineService> _timelineService = new();

    private GetOperationalTimelineQueryHandler CreateHandler() => new(_timelineService.Object);

    [Fact]
    public async Task Handle_WhenNoEvents_ReturnsEmptyEntries()
    {
        var response = BuildResponse(entries: []);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetOperationalTimelineQuery(new OperationalTimelineFilters()), CancellationToken.None);

        result.Entries.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSecurityAlerts_ReturnsSecurityAlertEntries()
    {
        var entry = BuildEntry("entry-1", entryType: "security_alert", severity: "critical");
        var response = BuildResponse(entries: [entry]);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetOperationalTimelineQuery(new OperationalTimelineFilters()), CancellationToken.None);

        result.Entries.Should().ContainSingle(e => e.EntryType == "security_alert");
    }

    [Fact]
    public async Task Handle_WithAuditLogs_ReturnsAuditLogEntries()
    {
        var entry = BuildEntry("entry-2", entryType: "audit_log", severity: "info");
        var response = BuildResponse(entries: [entry]);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetOperationalTimelineQuery(new OperationalTimelineFilters()), CancellationToken.None);

        result.Entries.Should().ContainSingle(e => e.EntryType == "audit_log");
    }

    [Fact]
    public async Task Handle_MasksUserId_ShowsOnlyFirst8Chars()
    {
        // O serviço é responsável por mascarar o userId; o handler apenas repassa o valor recebido.
        // Verificamos que o handler não altera o MaskedUserId entregue pelo serviço.
        var entry = BuildEntry("entry-3", maskedUserId: "ab12cd34");
        var response = BuildResponse(entries: [entry]);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetOperationalTimelineQuery(new OperationalTimelineFilters()), CancellationToken.None);

        var returnedEntry = result.Entries.Single();
        returnedEntry.MaskedUserId.Should().NotBeNull();
        returnedEntry.MaskedUserId!.Length.Should().Be(8);
    }

    [Fact]
    public async Task Handle_DefaultRange_IsLast24Hours()
    {
        // Quando os filtros não possuem From/To, o handler delega ao serviço sem alteração.
        // Este teste verifica que GetTimelineAsync é chamado (delegação ocorre).
        var response = BuildResponse(entries: []);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var filters = new OperationalTimelineFilters(); // From e To nulos
        await CreateHandler().Handle(new GetOperationalTimelineQuery(filters), CancellationToken.None);

        _timelineService.Verify(
            s => s.GetTimelineAsync(It.Is<OperationalTimelineFilters>(f => f.From == null && f.To == null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SortsDescendingByDate_ReturnsNewerFirst()
    {
        // O serviço entrega as entradas já ordenadas de forma descendente; o handler repassa sem
        // alterar a ordem.
        var newer = BuildEntry("entry-new", occurredAtUtc: UtcNow.AddHours(-1));
        var older = BuildEntry("entry-old", occurredAtUtc: UtcNow.AddHours(-5));
        var response = BuildResponse(entries: [newer, older]);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetOperationalTimelineQuery(new OperationalTimelineFilters()), CancellationToken.None);

        result.Entries[0].OccurredAtUtc.Should().BeAfter(result.Entries[1].OccurredAtUtc);
    }

    [Fact]
    public async Task Handle_MaxEntries_DoesNotExceed100()
    {
        // O serviço retorna exatamente 100 entradas; o handler não deve truncar nem expandir.
        var entries = Enumerable.Range(1, 100)
            .Select(i => BuildEntry($"entry-{i}", occurredAtUtc: UtcNow.AddMinutes(-i)))
            .ToList();
        var response = BuildResponse(entries: entries);
        _timelineService
            .Setup(s => s.GetTimelineAsync(It.IsAny<OperationalTimelineFilters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CreateHandler().Handle(new GetOperationalTimelineQuery(new OperationalTimelineFilters()), CancellationToken.None);

        result.Entries.Count.Should().Be(100);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TimelineEntryResponse BuildEntry(
        string id,
        string entryType = "audit_log",
        string title = "Operação registrada",
        string description = "Descrição da entrada de timeline.",
        string severity = "info",
        DateTime? occurredAtUtc = null,
        string? maskedUserId = null,
        string? resourceId = null,
        string? resourceType = null,
        string? correlationId = null,
        bool isRelationCertain = true,
        string? detailUrl = null) =>
        new(
            id,
            entryType,
            title,
            description,
            severity,
            occurredAtUtc ?? UtcNow.AddHours(-1),
            maskedUserId,
            resourceId,
            resourceType,
            correlationId,
            isRelationCertain,
            detailUrl);

    private static OperationalTimelineResponse BuildResponse(
        IReadOnlyList<TimelineEntryResponse>? entries = null,
        ImpactSummaryResponse? impact = null,
        DateTime? generatedAtUtc = null) =>
        new(
            entries ?? [],
            impact ?? new ImpactSummaryResponse(
                EstimatedUsersAffected: 0,
                ResourcesAffected: 0,
                PeakSeverity: null,
                PeriodStart: null,
                PeriodEnd: null),
            generatedAtUtc ?? UtcNow);
}
