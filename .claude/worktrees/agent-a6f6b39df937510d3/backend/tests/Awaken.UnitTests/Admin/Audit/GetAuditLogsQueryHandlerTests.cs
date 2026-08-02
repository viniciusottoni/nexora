using Awaken.Application.Admin.Audit.Queries.GetAuditLogs;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Audit;

/// <summary>US-166: testes do handler de listagem/filtro do log de auditoria administrativa.</summary>
public class GetAuditLogsQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();

    private GetAuditLogsQueryHandler CreateHandler() => new(_auditLogRepository.Object);

    private static AuditLog CreateEntry(
        string action = "AdminTicket.StatusChanged",
        AuditActorType actorType = AuditActorType.Admin,
        string resourceType = "SupportTicket") =>
        AuditLog.Create(action, Guid.NewGuid(), actorType, resourceType, Guid.NewGuid(), null, "corr-1", UtcNow);

    [Fact]
    public async Task Handle_FiltersPassThroughToRepository()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        _auditLogRepository
            .Setup(r => r.GetPagedAsync(
                "Admin", "AdminTicket.StatusChanged", "SupportTicket", from, to, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));

        var query = new GetAuditLogsQuery("Admin", "AdminTicket.StatusChanged", "SupportTicket", from, to, 1, 10);
        await CreateHandler().Handle(query, CancellationToken.None);

        _auditLogRepository.Verify(r => r.GetPagedAsync(
            "Admin", "AdminTicket.StatusChanged", "SupportTicket", from, to, 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PaginationFields_PassThroughToResponse()
    {
        var entries = Enumerable.Range(0, 5).Select(_ => CreateEntry()).ToList();

        _auditLogRepository
            .Setup(r => r.GetPagedAsync(
                null, null, null, null, null, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entries, 12));

        var result = await CreateHandler()
            .Handle(new GetAuditLogsQuery(null, null, null, null, null, 2, 5), CancellationToken.None);

        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Total.Should().Be(12);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_MapsActorTypeEnumToString()
    {
        var entry = CreateEntry(actorType: AuditActorType.System);

        _auditLogRepository
            .Setup(r => r.GetPagedAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog> { entry }, 1));

        var result = await CreateHandler()
            .Handle(new GetAuditLogsQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].ActorType.Should().Be("System");
    }

    [Fact]
    public async Task Handle_MapsAllSummaryFields()
    {
        var entry = CreateEntry();

        _auditLogRepository
            .Setup(r => r.GetPagedAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog> { entry }, 1));

        var result = await CreateHandler()
            .Handle(new GetAuditLogsQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        var item = result.Items[0];
        item.Id.Should().Be(entry.Id);
        item.ActorUserId.Should().Be(entry.ActorUserId);
        item.Action.Should().Be(entry.Action);
        item.ResourceType.Should().Be(entry.ResourceType);
        item.ResourceId.Should().Be(entry.ResourceId);
        item.CorrelationId.Should().Be(entry.CorrelationId);
        item.CreatedAtUtc.Should().Be(entry.CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _auditLogRepository
            .Setup(r => r.GetPagedAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));

        var result = await CreateHandler()
            .Handle(new GetAuditLogsQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }
}
