using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Queries.GetOnboardingStatus;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Users;

public class GetOnboardingStatusQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private GetOnboardingStatusQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _currentUserService.Object);

    [Fact]
    public async Task HandleReturnsGoalStepForNewUserWithoutProgress()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");

        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(
            new GetOnboardingStatusQuery(),
            CancellationToken.None);

        result.OnboardingCompleted.Should().BeFalse();
        result.CurrentStep.Should().Be("goal");
    }

    [Fact]
    public async Task HandleReturnsCompletedStepWhenOnboardingFinished()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.CompleteOnboarding(DateTime.UtcNow);

        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(
            new GetOnboardingStatusQuery(),
            CancellationToken.None);

        result.OnboardingCompleted.Should().BeTrue();
        result.CurrentStep.Should().Be("completed");
    }

    [Fact]
    public async Task HandleThrowsWhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetOnboardingStatusQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }
}
