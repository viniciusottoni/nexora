using Awaken.Application.Admin.Reports.Queries.GetOperationalReport;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Reports;

public class GetOperationalReportQueryHandlerTests
{
    private readonly Mock<IAdminAnalyticsRepository> _analyticsRepo = new();
    private readonly Mock<ISupportTicketRepository> _ticketRepo = new();
    private readonly Mock<IOperationalBugRepository> _bugRepo = new();
    private readonly Mock<ISecurityAlertRepository> _alertRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);

    public GetOperationalReportQueryHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        _analyticsRepo.Setup(r => r.CountUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _analyticsRepo.Setup(r => r.CountDistinctActiveUsersSinceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(10);
        _analyticsRepo.Setup(r => r.CountOpenSupportTicketsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _analyticsRepo.Setup(r => r.GetTopEventsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<(string Action, int Count)>)new List<(string, int)> { ("QuestCompleted", 42) });

        _ticketRepo.Setup(r => r.GetPagedAsync(null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SupportTicket>)new List<SupportTicket>(), 0));

        _bugRepo.Setup(r => r.GetPagedAsync(null, null, null, It.IsAny<string?>(), null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<OperationalBug>)new List<OperationalBug>(), 0));

        _alertRepo.Setup(r => r.GetPagedAsync(null, null, null, It.IsAny<string?>(), 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SecurityAlert>)new List<SecurityAlert>(), 0));
    }

    private GetOperationalReportQueryHandler CreateHandler() => new(
        _analyticsRepo.Object,
        _ticketRepo.Object,
        _bugRepo.Object,
        _alertRepo.Object,
        _dateTimeService.Object);

    [Fact]
    public async Task HandleReturnsReportWithCorrectPeriodDefaults()
    {
        var query = new GetOperationalReportQuery(null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.From.Should().Be(DateOnly.FromDateTime(UtcNow.Date.AddDays(-7)));
        result.To.Should().Be(DateOnly.FromDateTime(UtcNow));
        result.Environment.Should().Be("all");
    }

    [Fact]
    public async Task HandleReturnsCorrectTotalUsers()
    {
        var query = new GetOperationalReportQuery(null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.DailyOps.TotalUsers.Should().Be(100);
    }

    [Fact]
    public async Task HandleReturnsDauFromAnalyticsRepo()
    {
        var query = new GetOperationalReportQuery(null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.DailyOps.Dau.Should().Be(10);
        result.Product.Dau.Should().Be(10);
    }

    [Fact]
    public async Task HandleReturnsTopEventFromAnalyticsRepo()
    {
        var query = new GetOperationalReportQuery(null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Product.TopEventName.Should().Be("QuestCompleted");
        result.Product.TopEventCount.Should().Be(42);
    }

    [Fact]
    public async Task HandleUsesExplicitPeriodWhenProvided()
    {
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);

        var query = new GetOperationalReportQuery(from, to, "prod");
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.From.Should().Be(DateOnly.FromDateTime(from));
        result.To.Should().Be(DateOnly.FromDateTime(to));
        result.Environment.Should().Be("prod");
    }

    [Fact]
    public async Task HandleReturnsNullDauMauRatioWhenMauIsZero()
    {
        _analyticsRepo.Setup(r => r.CountDistinctActiveUsersSinceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOperationalReportQuery(null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Product.DauMauRatio.Should().BeNull();
    }
}
