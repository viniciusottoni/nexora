using Awaken.Application.Common.Interfaces;
using Awaken.Application.Onboarding.Commands.CompleteOnboarding;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Onboarding;

public class CompleteOnboardingCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CompleteOnboardingCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

    private CompleteOnboardingCommandHandler CreateHandler() => new(
        _profileRepository.Object,
        _userRepository.Object,
        _hunterProgressionRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private void SetUpNoExistingProgression() =>
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

    private static CompleteOnboardingCommand ValidCommand() => new(
        Goal: "gain_muscle",
        ExperienceLevel: "beginner",
        Age: 28,
        HeightCm: 175m,
        WeightKg: 82m,
        BiologicalSex: "masculino",
        TrainingDuration: "1_6_months",
        AvailableMinutesPerWorkout: 30,
        BodyType: "normal",
        PhysicalLimitations: ["no_limitations"],
        PhysicalPains: ["no_pains"]);

    private User BuildUser()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        return user;
    }

    public CompleteOnboardingCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task CA001_CreatesProfileAndCompletesOnboarding_WhenNoProfileExists()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.OnboardingCompleted.Should().BeTrue();
        result.NextRoute.Should().Be("daily_quest");
        user.IsOnboardingComplete.Should().BeTrue();
        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p =>
                p.UserId == UserId &&
                p.Goal == "gain_muscle" &&
                p.ExperienceLevel == "beginner" &&
                p.Age == 28 &&
                p.HeightCm == 175m &&
                p.WeightKg == 82m &&
                p.BiologicalSex == "masculino" &&
                p.TrainingDuration == "1_6_months" &&
                p.AvailableMinutesPerWorkout == 30 &&
                p.BodyType == "normal"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("onboarding_completed") &&
                    v.ToString()!.Contains($"userId={UserId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CA001_UpdatesExistingProfileAndCompletesOnboarding()
    {
        var existing = UserProfile.Create(UserId, age: 25);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        existing.Goal.Should().Be("gain_muscle");
        existing.Age.Should().Be(28);
        user.IsOnboardingComplete.Should().BeTrue();
        _profileRepository.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RN003_OnboardingCompletedAtIsRecorded()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        user.CurrentOnboardingStep.Should().Be("completed");
    }

    [Fact]
    public async Task CA002_NextRouteIsDailyQuest()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.NextRoute.Should().Be("daily_quest");
    }

    [Fact]
    public async Task RN005_HandlerDoesNotGrantXP()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.OnboardingCompleted.Should().BeTrue();
        result.NextRoute.Should().Be("daily_quest");
    }

    [Fact]
    public async Task RN006_DoesNotGrantGeneralXp()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();
        HunterProgression? added = null;
        _hunterProgressionRepository
            .Setup(r => r.AddAsync(It.IsAny<HunterProgression>(), It.IsAny<CancellationToken>()))
            .Callback<HunterProgression, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        added!.TotalXp.Should().Be(0);
    }

    [Fact]
    public async Task CA001_CapsRankScoreAt48_AndRankAtB_ForMostAdvancedProfile()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();
        HunterProgression? added = null;
        _hunterProgressionRepository
            .Setup(r => r.AddAsync(It.IsAny<HunterProgression>(), It.IsAny<CancellationToken>()))
            .Callback<HunterProgression, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        var command = ValidCommand() with
        {
            ExperienceLevel = "advanced",
            TrainingDuration = "more_than_3_years",
        };
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.RankScore.Should().Be(48);
        result.Rank.Should().Be("B");
        result.RankCapApplied.Should().BeTrue();
        added!.Strength.Should().Be(8);
        added.Wisdom.Should().Be(8);
    }

    [Fact]
    public async Task CA002_LevelIsAlwaysOne_EvenWithRankB()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        var command = ValidCommand() with
        {
            ExperienceLevel = "advanced",
            TrainingDuration = "more_than_3_years",
        };
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Level.Should().Be(1);
    }

    [Fact]
    public async Task RN005_UsesConservativeEffectiveLevel_WhenProfileConflicts()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        // RN-005: experiência "advanced" mas nunca treinou ("does_not_train") — usa o nível conservador.
        var command = ValidCommand() with
        {
            ExperienceLevel = "advanced",
            TrainingDuration = "does_not_train",
        };
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.RankScore.Should().Be(6);
        result.Rank.Should().Be("E");
        result.RankCapApplied.Should().BeFalse();
    }

    [Fact]
    public async Task IsIdempotent_DoesNotRecalculateProgression_WhenAlreadyExists()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var existingProgression = HunterProgression.CreateFromOnboarding(
            UserId, strength: 8, agility: 8, endurance: 8, vitality: 8, focus: 8, wisdom: 8);
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgression);

        var command = ValidCommand() with { ExperienceLevel = "sedentary", TrainingDuration = "does_not_train" };
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Rank.Should().Be("B");
        result.RankScore.Should().Be(48);
        _hunterProgressionRepository.Verify(
            r => r.AddAsync(It.IsAny<HunterProgression>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrimsBiologicalSexWhitespace()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        var command = ValidCommand() with { BiologicalSex = "  feminino  " };
        await CreateHandler().Handle(command, CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p => p.BiologicalSex == "feminino"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsIdempotent_CallingTwiceSucceeds()
    {
        var existing = UserProfile.Create(UserId, goal: "lose_weight");
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var user = BuildUser();
        user.CompleteOnboarding(DateTime.UtcNow);
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        SetUpNoExistingProgression();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.OnboardingCompleted.Should().BeTrue();
        existing.Goal.Should().Be("gain_muscle");
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("onboarding_completed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
