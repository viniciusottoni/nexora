using Awaken.Application.Admin.Dashboard.Queries.GetAdminDashboard;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Admin.Dashboard;

/// <summary>
/// US-161 — testes do handler de dashboard administrativo.
///
/// CA: banco vazio retorna zeros sem lançar exceção.
/// CA: bloco com falha não derruba o resultado inteiro (RN-005).
/// CA: feliz caminho com dados semeados produz contagens corretas.
/// </summary>
public class GetAdminDashboardQueryHandlerTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    public GetAdminDashboardQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeServiceForContext = new Mock<IDateTimeService>();
        dateTimeServiceForContext.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeServiceForContext.Object);

        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    public void Dispose() => _context.Dispose();

    private GetAdminDashboardQueryHandler CreateHandler()
    {
        IAdminAnalyticsRepository repository = new AdminAnalyticsRepository(_context);
        return new GetAdminDashboardQueryHandler(
            repository,
            _dateTimeService.Object,
            NullLogger<GetAdminDashboardQueryHandler>.Instance);
    }

    private static void SetCreatedAtUtc(object entity, DateTime createdAtUtc)
    {
        entity.GetType().BaseType!
            .GetProperty(nameof(Awaken.Domain.Common.BaseEntity.CreatedAtUtc))!
            .SetValue(entity, createdAtUtc);
    }

    [Fact]
    public async Task Handler_WhenDatabaseIsEmpty_ReturnsZerosWithoutThrowing()
    {
        var act = async () => await CreateHandler().Handle(new GetAdminDashboardQuery(null, null), CancellationToken.None);

        var result = await act.Should().NotThrowAsync();

        result.Which.TotalUsers.Should().Be(0);
        result.Which.Dau.Should().Be(0);
        result.Which.OpenTickets.Should().Be(0);
        result.Which.Mrr.Should().BeNull("RN-002: sem fonte confiável de MRR, deve ser null");
        result.Which.DauTimeSeries.Should().BeEmpty();
        result.Which.RecentActivity.Should().BeEmpty();
        result.Which.TopEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handler_WithSeededData_ReturnsCorrectCounts()
    {
        var user1 = User.Create("hunter1@awaken.app", "hash");
        var user2 = User.Create("hunter2@awaken.app", "hash");
        _context.Users.AddRange(user1, user2);

        var ticketOpen = SupportTicket.Create(user1.Id, "report", "pt-BR", "bug", null, null, UtcNow);
        var ticketClosed = SupportTicket.Create(user2.Id, "report", "pt-BR", "bug", null, null, UtcNow);
        ticketClosed.UpdateStatus("closed", UtcNow);
        _context.SupportTickets.AddRange(ticketOpen, ticketClosed);

        var auditToday = AuditLog.Create(
            "quest_completed", user1.Id, AuditActorType.User, "Quest", null, null, "corr-1", UtcNow);
        var auditYesterday = AuditLog.Create(
            "quest_completed", user2.Id, AuditActorType.User, "Quest", null, null, "corr-2", UtcNow.AddDays(-1));
        _context.AuditLogs.AddRange(auditToday, auditYesterday);

        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetAdminDashboardQuery(null, null), CancellationToken.None);

        result.TotalUsers.Should().Be(2);
        result.Dau.Should().Be(1, "apenas user1 teve atividade hoje (>= UtcNow.Date)");
        result.OpenTickets.Should().Be(1);
        result.TopEvents.Should().ContainSingle(e => e.EventName == "quest_completed" && e.Count == 2);
        result.RecentActivity.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handler_DauTimeSeries_GroupsDistinctActorsPerDay()
    {
        var user1 = User.Create("hunter1@awaken.app", "hash");
        var user2 = User.Create("hunter2@awaken.app", "hash");
        _context.Users.AddRange(user1, user2);

        var log1 = AuditLog.Create("login", user1.Id, AuditActorType.User, "Session", null, null, null, UtcNow.AddDays(-2));
        var log2 = AuditLog.Create("login", user2.Id, AuditActorType.User, "Session", null, null, null, UtcNow.AddDays(-2));
        var log3 = AuditLog.Create("login", user1.Id, AuditActorType.User, "Session", null, null, null, UtcNow.AddDays(-1));
        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetAdminDashboardQuery(UtcNow.AddDays(-7), UtcNow), CancellationToken.None);

        result.DauTimeSeries.Should().HaveCount(2);
        var twoDaysAgo = result.DauTimeSeries.Single(p => p.Date == DateOnly.FromDateTime(UtcNow.AddDays(-2)));
        twoDaysAgo.Count.Should().Be(2, "dois usuários distintos ativos dois dias atrás");
    }

    [Fact]
    public async Task Handler_WhenDefaultPeriodNotProvided_UsesLast7Days()
    {
        // RN-004: periodo padrao deve priorizar visao recente (ultimos 7 dias).
        var user = User.Create("hunter@awaken.app", "hash");
        _context.Users.Add(user);

        var oldLog = AuditLog.Create("login", user.Id, AuditActorType.User, "Session", null, null, null, UtcNow.AddDays(-10));
        var recentLog = AuditLog.Create("login", user.Id, AuditActorType.User, "Session", null, null, null, UtcNow.AddDays(-2));
        _context.AuditLogs.AddRange(oldLog, recentLog);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetAdminDashboardQuery(null, null), CancellationToken.None);

        result.DauTimeSeries.Should().ContainSingle(
            "apenas o log de 2 dias atrás está dentro da janela padrão de 7 dias");
    }
}
