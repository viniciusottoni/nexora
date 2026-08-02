using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Onboarding.Commands.UpdateUserProfile;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Onboarding;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

    private UpdateUserProfileCommandHandler CreateHandler() => new(
        _profileRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static UpdateUserProfileCommand ValidCommand() => new(
        Goal: "gain_strength",
        ExperienceLevel: "intermediate",
        Age: 30,
        HeightCm: 180m,
        WeightKg: 80m,
        BiologicalSex: "masculino",
        TrainingDuration: "6_12_months",
        TrainingLocation: "gym",
        EquipmentAvailable: ["dumbbells", "full_gym"],
        AvailableMinutesPerWorkout: 40,
        AvailableDaysPerWeek: 4,
        BodyType: "athletic_strong",
        PhysicalLimitations: ["knee_problem"],
        PhysicalPains: ["lower_back"],
        TrainingPreferences: ["low_impact", "strength_focus"]);

    public UpdateUserProfileCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task CA001_UpdatesExistingProfile_AndPersists()
    {
        var existing = UserProfile.Create(UserId, goal: "lose_weight", age: 25);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        existing.Goal.Should().Be("gain_strength");
        existing.ExperienceLevel.Should().Be("intermediate");
        existing.EffectiveExperienceLevel.Should().Be("intermediate");
        existing.TrainingLocation.Should().Be("gym");
        existing.EquipmentAvailable.Should().BeEquivalentTo(["dumbbells", "full_gym"]);
        existing.Age.Should().Be(30);
        existing.AvailableDaysPerWeek.Should().Be(4);
        existing.BodyType.Should().Be("athletic_strong");
        existing.TrainingPreferences.Should().BeEquivalentTo(["low_impact", "strength_focus"]);
        result.Goal.Should().Be("gain_strength");
        result.TrainingLocation.Should().Be("gym");
        result.Age.Should().Be(30);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenProfileDoesNotExist()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RN003_OnlyPatchesSuppliedFields_LeavesOthersUntouched()
    {
        var existing = UserProfile.Create(
            UserId, goal: "lose_weight", experienceLevel: "beginner", bodyType: "lean");
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new UpdateUserProfileCommand(
            Goal: "gain_strength",
            ExperienceLevel: null,
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            TrainingLocation: null,
            EquipmentAvailable: null,
            AvailableMinutesPerWorkout: null,
            AvailableDaysPerWeek: null,
            BodyType: null,
            PhysicalLimitations: null,
            PhysicalPains: null,
            TrainingPreferences: null);

        await CreateHandler().Handle(command, CancellationToken.None);

        existing.Goal.Should().Be("gain_strength");
        existing.ExperienceLevel.Should().Be("beginner");
        existing.BodyType.Should().Be("lean");
    }

    [Fact]
    public async Task RN004_DoesNotTouchUnrelatedRepositories()
    {
        var existing = UserProfile.Create(UserId);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
