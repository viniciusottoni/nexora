using System.Reflection;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Hunter.Queries.GetHunterProgress;
using Awaken.Contracts.Progression;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Hunter;

public class GetHunterProgressQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly Guid _userId = Guid.NewGuid();

    public GetHunterProgressQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
    }

    private GetHunterProgressQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _hunterProgressionRepository.Object,
        _currentUserService.Object);

    [Fact]
    public async Task HandleThrowsUnauthorizedWhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetHunterProgressQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleReturnsBaselineProgressWhenProgressionDoesNotExist()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProgressQuery(), CancellationToken.None);

        result.UserId.Should().Be(user.Id);
        result.Rank.Should().Be("E");
        result.RankScore.Should().Be(6);
        result.Level.Should().Be(1);
        result.TotalXp.Should().Be(0);
        result.XpToNextLevel.Should().Be(100);
        result.CurrentStreakDays.Should().Be(0);
        result.LongestStreakDays.Should().Be(0);
        result.Attributes.Should().BeEquivalentTo(new AttributesDto(1, 1, 1, 1, 1, 1));
    }

    [Fact]
    public async Task HandleReturnsCurrentProgressWhenProgressionExists()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var progression = HunterProgression.Create(_userId);
        progression.AddXp(150, new DateTime(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc));
        progression.AddAttributePoints(4, 2, 2, 2, 1, 1, new DateTime(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new GetHunterProgressQuery(), CancellationToken.None);

        result.Rank.Should().Be(progression.Rank);
        result.RankScore.Should().Be(progression.RankScore);
        result.Level.Should().Be(progression.Level);
        result.TotalXp.Should().Be(progression.TotalXp);
        result.XpToNextLevel.Should().Be(progression.XpToNextLevel);
        result.Attributes.Should().BeEquivalentTo(new AttributesDto(
            progression.Strength,
            progression.Endurance,
            progression.Agility,
            progression.Vitality,
            progression.Focus,
            progression.Wisdom));
    }
}
