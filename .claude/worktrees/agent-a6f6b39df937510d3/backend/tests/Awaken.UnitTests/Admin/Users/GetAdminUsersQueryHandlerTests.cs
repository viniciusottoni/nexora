using Awaken.Application.Admin.Users.Queries.GetAdminUsers;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Users;

public class GetAdminUsersQueryHandlerTests
{
    private readonly Mock<IAdminUserQueryRepository> _repo = new();

    private static AdminUserRow MakeRow(string email, string? plan = null, string? status = null) =>
        new(
            Id: Guid.NewGuid(),
            Email: email,
            DisplayName: "Hunter",
            AvatarUrl: null,
            PreferredLanguage: "pt-BR",
            IsEmailVerified: true,
            IsOnboardingComplete: true,
            AuthProvider: "Local",
            LastLoginAtUtc: DateTime.UtcNow,
            TrialEndsAt: null,
            CreatedAtUtc: DateTime.UtcNow,
            Plan: plan,
            SubscriptionStatus: status,
            SubscriptionExpiresAt: null);

    private GetAdminUsersQueryHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task HandleReturnsMappedPagedList()
    {
        var rows = new List<AdminUserRow>
        {
            MakeRow("a@test.com", "monthly", "subscription_active"),
            MakeRow("b@test.com", null, null),
        };

        _repo.Setup(r => r.GetPagedAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)rows, 2));

        var query = new GetAdminUsersQuery(null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items[0].Email.Should().Be("a@test.com");
        result.Items[0].Plan.Should().Be("monthly");
    }

    [Fact]
    public async Task HandleWithEmptySearchReturnsAll()
    {
        var rows = new List<AdminUserRow> { MakeRow("x@test.com") };

        _repo.Setup(r => r.GetPagedAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)rows, 1));

        var query = new GetAdminUsersQuery(null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandlePropagatesFiltersToRepository()
    {
        _repo.Setup(r => r.GetPagedAsync("john", "monthly", "subscription_active", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)new List<AdminUserRow>(), 0));

        var query = new GetAdminUsersQuery("john", "monthly", "subscription_active", 2, 10);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetPagedAsync("john", "monthly", "subscription_active", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task HandleReturnsEmptyListWhenNoResults()
    {
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AdminUserRow>)new List<AdminUserRow>(), 0));

        var query = new GetAdminUsersQuery("notfound", null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }
}
