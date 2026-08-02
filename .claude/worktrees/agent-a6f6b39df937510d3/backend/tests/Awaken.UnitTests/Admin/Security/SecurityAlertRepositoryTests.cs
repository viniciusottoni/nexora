using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Security;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Awaken.UnitTests.Admin.Security;

/// <summary>
/// US-219 RN-002: alertas críticos devem aparecer antes de alertas baixos na ordenação
/// padrão da listagem, independentemente da data de criação.
/// </summary>
public class SecurityAlertRepositoryTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;
    private readonly SecurityAlertRepository _repository;

    public SecurityAlertRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeService.Object);
        _repository = new SecurityAlertRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetPagedAsync_OrdersCriticalAlertsBeforeLowSeverity_RegardlessOfCreationDate()
    {
        // Alerta "low" criado depois (mais recente) do que o "critical" — sem RN-002 o low viria primeiro.
        var oldCritical = SecurityAlert.Create("brute_force", "critical", "prod", UtcNow.AddHours(-5));
        var newLow = SecurityAlert.Create("suspicious_traffic", "low", "prod", UtcNow);
        var newMedium = SecurityAlert.Create("invalid_token", "medium", "prod", UtcNow.AddMinutes(-1));
        var newHigh = SecurityAlert.Create("rbac_denied", "high", "prod", UtcNow.AddMinutes(-2));

        _context.SecurityAlerts.AddRange(oldCritical, newLow, newMedium, newHigh);
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(null, null, null, null, 1, 20);

        total.Should().Be(4);
        items.Select(i => i.Severity).Should().ContainInOrder("critical", "high", "medium", "low");
    }

    [Fact]
    public async Task GetByIdAsync_AfterClassifyAsFalsePositive_StillReturnsAlert()
    {
        // RN-005: classificar como falso positivo não remove o alerta do repositório.
        var alert = SecurityAlert.Create("brute_force", "high", "prod", UtcNow);
        _context.SecurityAlerts.Add(alert);
        await _context.SaveChangesAsync();

        alert.Classify("false_positive", Guid.NewGuid(), UtcNow);
        await _context.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(alert.Id);

        found.Should().NotBeNull();
        found!.Classification.Should().Be("false_positive");
        found.IsDeleted.Should().BeFalse();
    }
}
