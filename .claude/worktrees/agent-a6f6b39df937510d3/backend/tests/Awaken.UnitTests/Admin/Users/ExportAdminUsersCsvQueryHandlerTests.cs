using System.Text;
using Awaken.Application.Admin.Users.Queries.ExportAdminUsersCsv;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Users;

public class ExportAdminUsersCsvQueryHandlerTests
{
    private readonly Mock<IAdminUserQueryRepository> _repo = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid AdminId = Guid.NewGuid();

    public ExportAdminUsersCsvQueryHandlerTests()
    {
        _currentAdminService.Setup(s => s.AdminUserId).Returns(AdminId);
    }

    private ExportAdminUsersCsvQueryHandler CreateHandler() => new(
        _repo.Object,
        _currentAdminService.Object,
        _auditLogService.Object,
        _unitOfWork.Object);

    private static AdminUserRow MakeRow(string email) =>
        new(
            Id: Guid.NewGuid(),
            Email: email,
            DisplayName: "Hunter",
            AvatarUrl: null,
            PreferredLanguage: "pt-BR",
            IsEmailVerified: true,
            IsOnboardingComplete: false,
            AuthProvider: "Local",
            LastLoginAtUtc: new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            TrialEndsAt: null,
            CreatedAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Plan: "monthly",
            SubscriptionStatus: "subscription_active",
            SubscriptionExpiresAt: null);

    [Fact]
    public async Task HandleReturnsBytesWithExpectedCsvHeaders()
    {
        _repo.Setup(r => r.GetPagedAsync(null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)new List<AdminUserRow> { MakeRow("test@test.com") }, 1));

        var bytes = await CreateHandler().Handle(new ExportAdminUsersCsvQuery(null, null, null), CancellationToken.None);

        var csv = Encoding.UTF8.GetString(bytes);
        csv.Should().StartWith("Id,Email,DisplayName,Plan,SubscriptionStatus,IsEmailVerified,IsOnboardingComplete,LastLoginAtUtc,CreatedAtUtc");
    }

    [Fact]
    public async Task HandleCsvDoesNotContainPasswordOrTokenColumns()
    {
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)new List<AdminUserRow> { MakeRow("user@test.com") }, 1));

        var bytes = await CreateHandler().Handle(new ExportAdminUsersCsvQuery(null, null, null), CancellationToken.None);

        var csv = Encoding.UTF8.GetString(bytes);
        csv.Should().NotContain("Password");
        csv.Should().NotContain("Token");
        csv.Should().NotContain("PasswordHash");
        csv.Should().NotContain("AvatarUrl");
    }

    [Fact]
    public async Task HandleRecordsAuditAfterExport()
    {
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)new List<AdminUserRow>(), 0));

        await CreateHandler().Handle(new ExportAdminUsersCsvQuery(null, null, null), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminUsersExported,
            AdminId,
            AuditActorType.Admin,
            AuditResourceTypes.AdminUser,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleContainsUserDataInCsvRows()
    {
        var row = MakeRow("export@test.com");

        _repo.Setup(r => r.GetPagedAsync(null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)new List<AdminUserRow> { row }, 1));

        var bytes = await CreateHandler().Handle(new ExportAdminUsersCsvQuery(null, null, null), CancellationToken.None);

        var csv = Encoding.UTF8.GetString(bytes);
        csv.Should().Contain("export@test.com");
        csv.Should().Contain("monthly");
        csv.Should().Contain("subscription_active");
    }
}
