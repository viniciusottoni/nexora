using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Queries.GetUserProfile;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Onboarding;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private GetUserProfileQueryHandler CreateHandler() => new(
        _profileRepository.Object,
        _currentUserService.Object);

    public GetUserProfileQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    [Fact]
    public async Task ReturnsProfile_WhenItExists()
    {
        var profile = UserProfile.Create(
            UserId,
            goal: "gain_muscle",
            experienceLevel: "beginner",
            age: 28,
            heightCm: 175m,
            weightKg: 82m,
            biologicalSex: "masculino",
            trainingDuration: "1_6_months",
            availableMinutesPerWorkout: 30,
            bodyType: "normal",
            physicalLimitations: ["no_limitations"],
            physicalPains: ["no_pains"]);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await CreateHandler().Handle(new GetUserProfileQuery(), CancellationToken.None);

        result.Goal.Should().Be("gain_muscle");
        result.ExperienceLevel.Should().Be("beginner");
        result.EffectiveExperienceLevel.Should().Be("beginner");
        result.Age.Should().Be(28);
        result.HeightCm.Should().Be(175m);
        result.WeightKg.Should().Be(82m);
        result.BiologicalSex.Should().Be("masculino");
        result.TrainingDuration.Should().Be("1_6_months");
        result.AvailableMinutesPerWorkout.Should().Be(30);
        result.BodyType.Should().Be("normal");
        result.PhysicalLimitations.Should().BeEquivalentTo(new[] { "no_limitations" });
        result.PhysicalPains.Should().BeEquivalentTo(new[] { "no_pains" });
    }

    [Fact]
    public async Task ThrowsNotFound_WhenProfileDoesNotExist()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var act = () => CreateHandler().Handle(new GetUserProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
