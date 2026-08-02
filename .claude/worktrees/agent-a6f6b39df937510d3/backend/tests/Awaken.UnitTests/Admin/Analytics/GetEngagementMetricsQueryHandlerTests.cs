using Awaken.Application.Admin.Analytics.Queries.GetEngagementMetrics;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Awaken.UnitTests.Admin.Analytics;

/// <summary>
/// US-169 — testes do handler de métricas de engajamento e retenção.
///
/// CA: nenhum usuário velho o suficiente para D30 → InsufficientData=true (RN-003).
/// CA: usuários velhos o suficiente com atividade → taxa de retenção computada.
/// </summary>
public class GetEngagementMetricsQueryHandlerTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    public GetEngagementMetricsQueryHandlerTests()
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

    private GetEngagementMetricsQueryHandler CreateHandler()
    {
        IAdminAnalyticsRepository repository = new AdminAnalyticsRepository(_context);
        return new GetEngagementMetricsQueryHandler(repository, _dateTimeService.Object);
    }

    private static void SetCreatedAtUtc(User user, DateTime createdAtUtc) =>
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAtUtc))!.SetValue(user, createdAtUtc);

    [Fact]
    public async Task Handler_WhenNoUsersOldEnoughForD30_ReturnsInsufficientData()
    {
        // Único usuário registrado há apenas 5 dias — não há coorte de 30 dias ainda.
        var user = User.Create("hunter@awaken.app", "hash");
        SetCreatedAtUtc(user, UtcNow.AddDays(-5));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetEngagementMetricsQuery("registration", null, null), CancellationToken.None);

        result.RetentionD30!.InsufficientData.Should().BeTrue("RN-003: nenhum usuário tem 30 dias de idade ainda");
        result.RetentionD30!.RetentionRate.Should().BeNull();
    }

    [Fact]
    public async Task Handler_WhenUsersOldEnoughWithActivity_ComputesRetentionRate()
    {
        var retainedUser = User.Create("retained@awaken.app", "hash");
        SetCreatedAtUtc(retainedUser, UtcNow.AddDays(-31));

        var churnedUser = User.Create("churned@awaken.app", "hash");
        SetCreatedAtUtc(churnedUser, UtcNow.AddDays(-35));

        _context.Users.AddRange(retainedUser, churnedUser);

        // Atividade do retainedUser ocorre 32 dias atrás (> CreatedAtUtc + 30 dias).
        var activity = AuditLog.Create(
            "quest_completed", retainedUser.Id, AuditActorType.User, "Quest", null, null, null,
            UtcNow.AddDays(-31).AddDays(31));
        _context.AuditLogs.Add(activity);

        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetEngagementMetricsQuery("registration", null, null), CancellationToken.None);

        result.RetentionD30!.InsufficientData.Should().BeFalse();
        result.RetentionD30!.RetentionRate.Should().Be(0.5, "1 de 2 usuários elegíveis teve atividade após D30");
    }

    [Fact]
    public async Task Handler_DauMauRatio_IsNullWhenNoActiveUsers()
    {
        var result = await CreateHandler().Handle(
            new GetEngagementMetricsQuery("registration", null, null), CancellationToken.None);

        result.DauMauRatio.Should().BeNull();
        result.Dau.Should().Be(0);
        result.Mau.Should().Be(0);
    }

    [Fact]
    public async Task Handler_FeatureUsage_GroupsByActionDescendingByCount()
    {
        var user = User.Create("hunter@awaken.app", "hash");
        _context.Users.Add(user);

        _context.AuditLogs.AddRange(
            AuditLog.Create("quest_completed", user.Id, AuditActorType.User, "Quest", null, null, null, UtcNow.AddDays(-1)),
            AuditLog.Create("quest_completed", user.Id, AuditActorType.User, "Quest", null, null, null, UtcNow.AddDays(-1)),
            AuditLog.Create("profile_updated", user.Id, AuditActorType.User, "Profile", null, null, null, UtcNow.AddDays(-1)));

        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetEngagementMetricsQuery("registration", null, null), CancellationToken.None);

        result.FeatureUsage.Should().NotBeEmpty();
        result.FeatureUsage.First().Feature.Should().Be("quest_completed");
        result.FeatureUsage.First().UsageCount.Should().Be(2);
    }
}
