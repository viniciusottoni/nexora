using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Queries.GetEffectiveExperienceLevel;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Onboarding;

public class GetEffectiveExperienceLevelQueryHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private GetEffectiveExperienceLevelQueryHandler CreateHandler() => new(
        _profileRepository.Object,
        _currentUserService.Object);

    public GetEffectiveExperienceLevelQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    [Fact]
    public async Task CA001_ReturnsConservativeEffectiveLevel_WhenExperienceAndDurationConflict()
    {
        var profile = UserProfile.Create(
            UserId,
            experienceLevel: "advanced",
            trainingDuration: "does_not_train");
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await CreateHandler().Handle(new GetEffectiveExperienceLevelQuery(), CancellationToken.None);

        result.ExperienceLevel.Should().Be("advanced");
        result.TrainingDuration.Should().Be("does_not_train");
        result.EffectiveExperienceLevel.Should().Be("sedentary");
    }

    [Fact]
    public async Task ThrowsNotFound_WhenProfileDoesNotExist()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var act = () => CreateHandler().Handle(new GetEffectiveExperienceLevelQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
