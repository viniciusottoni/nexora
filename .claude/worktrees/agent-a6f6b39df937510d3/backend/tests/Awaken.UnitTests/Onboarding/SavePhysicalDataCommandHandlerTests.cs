using Awaken.Application.Common.Interfaces;
using Awaken.Application.Onboarding.Commands.SavePhysicalData;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Onboarding;

public class SavePhysicalDataCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid _userId = Guid.NewGuid();
    private static readonly DateTime _utcNow = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

    private SavePhysicalDataCommandHandler CreateHandler() => new(
        _profileRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static SavePhysicalDataCommand ValidCommand() =>
        new(
            Age: 28,
            HeightCm: 175m,
            WeightKg: 82m,
            BiologicalSex: "masculino",
            TrainingDuration: "1_6_months",
            AvailableMinutesPerWorkout: 30,
            BodyType: null,
            PhysicalLimitations: null,
            PhysicalPains: null);

    public SavePhysicalDataCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(_utcNow);
    }

    [Fact]
    public async Task CreatesNewProfileWhenNoneExists()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(Unit.Value);
        _profileRepository.Verify(r => r.AddAsync(It.Is<UserProfile>(p =>
            p.UserId == _userId &&
            p.Age == 28 &&
            p.HeightCm == 175m &&
            p.WeightKg == 82m &&
            p.BiologicalSex == "masculino" &&
            p.TrainingDuration == "1_6_months" &&
            p.AvailableMinutesPerWorkout == 30),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatesExistingProfileWhenFound()
    {
        var existing = UserProfile.Create(
            userId: _userId,
            age: 25,
            heightCm: 170m,
            weightKg: 75m,
            biologicalSex: "feminino",
            trainingDuration: "does_not_train",
            availableMinutesPerWorkout: 10);
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = ValidCommand();
        await CreateHandler().Handle(command, CancellationToken.None);

        existing.Age.Should().Be(28);
        existing.HeightCm.Should().Be(175m);
        existing.WeightKg.Should().Be(82m);
        existing.BiologicalSex.Should().Be("masculino");
        existing.TrainingDuration.Should().Be("1_6_months");
        existing.AvailableMinutesPerWorkout.Should().Be(30);
        _profileRepository.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrimsWhitespaceFromBiologicalSex()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var command = ValidCommand() with { BiologicalSex = "  nao-binario  " };
        await CreateHandler().Handle(command, CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p => p.BiologicalSex == "nao-binario"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PartialPatchPreservesExistingPhysicalData()
    {
        var existing = UserProfile.Create(
            userId: _userId,
            age: 25,
            heightCm: 170m,
            weightKg: 75m,
            biologicalSex: "feminino",
            trainingDuration: "does_not_train",
            availableMinutesPerWorkout: 10);
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: "more_than_3_years",
            AvailableMinutesPerWorkout: 50,
            BodyType: null,
            PhysicalLimitations: null,
            PhysicalPains: null);

        await CreateHandler().Handle(command, CancellationToken.None);

        existing.Age.Should().Be(25);
        existing.HeightCm.Should().Be(170m);
        existing.WeightKg.Should().Be(75m);
        existing.BiologicalSex.Should().Be("feminino");
        existing.TrainingDuration.Should().Be("more_than_3_years");
        existing.AvailableMinutesPerWorkout.Should().Be(50);
        existing.BodyType.Should().BeNull();
    }

    [Fact]
    public async Task US141_BodyTypeIsSavedWhenProvided()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var command = ValidCommand() with { BodyType = "athletic_strong" };
        await CreateHandler().Handle(command, CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p => p.BodyType == "athletic_strong"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task US141_BodyTypePatchPreservesOtherFields()
    {
        var existing = UserProfile.Create(
            userId: _userId,
            age: 25,
            heightCm: 170m,
            weightKg: 75m,
            biologicalSex: "feminino",
            trainingDuration: "does_not_train",
            availableMinutesPerWorkout: 30,
            bodyType: null);
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: "lean",
            PhysicalLimitations: null,
            PhysicalPains: null);

        await CreateHandler().Handle(command, CancellationToken.None);

        existing.BodyType.Should().Be("lean");
        existing.Age.Should().Be(25);
        existing.BiologicalSex.Should().Be("feminino");
    }

    [Fact]
    public async Task US142_PhysicalLimitationsAreSavedOnCreate()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: null,
            PhysicalLimitations: ["knee_problem", "no_impact"],
            PhysicalPains: null);

        await CreateHandler().Handle(command, CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p =>
                p.PhysicalLimitations != null &&
                p.PhysicalLimitations.Contains("knee_problem") &&
                p.PhysicalLimitations.Contains("no_impact")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task US142_PhysicalLimitationsArePatchedOnExistingProfile()
    {
        var existing = UserProfile.Create(
            userId: _userId,
            age: 28,
            heightCm: 175m,
            weightKg: 82m,
            biologicalSex: "masculino");
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: null,
            PhysicalLimitations: ["shoulder_injury"],
            PhysicalPains: null);

        await CreateHandler().Handle(command, CancellationToken.None);

        existing.PhysicalLimitations.Should().ContainSingle().Which.Should().Be("shoulder_injury");
        existing.Age.Should().Be(28);
    }

    [Fact]
    public async Task US142_NoLimitationsSavesSingleExclusiveTag()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: null,
            PhysicalLimitations: ["no_limitations"],
            PhysicalPains: null);

        await CreateHandler().Handle(command, CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p =>
                p.PhysicalLimitations != null &&
                p.PhysicalLimitations.SequenceEqual(new[] { "no_limitations" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
