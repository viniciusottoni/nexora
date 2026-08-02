using Awaken.Application.Common.Interfaces;
using Awaken.Application.TrainingPrograms.Queries.GetTrainingPrograms;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.TrainingPrograms;

public class GetTrainingProgramsQueryHandlerTests
{
    private readonly Mock<ITrainingProgramRepository> _trainingProgramRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IUserWorkoutPreferenceRepository> _userWorkoutPreferenceRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);

    public GetTrainingProgramsQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetTrainingProgramsQueryHandler CreateHandler() => new(
        _trainingProgramRepository.Object,
        _hunterProgressionRepository.Object,
        _userWorkoutPreferenceRepository.Object,
        _currentUserService.Object);

    private static TrainingProgram FullBody() => TrainingProgram.Create(
        TrainingProgramKeys.FullBody, "Full Body", "Corpo inteiro em cada sessao.", "Sedentario", "E+", 1, UtcNow);

    private static TrainingProgram Abc() => TrainingProgram.Create(
        TrainingProgramKeys.Abc, "ABC", "Classico das academias.", "Intermediario", "C+", 3, UtcNow);

    private static TrainingProgram Ab() => TrainingProgram.Create(
        TrainingProgramKeys.Ab, "AB", "Push + Pull.", "Sedentario, Iniciante", "D+", 2, UtcNow);

    [Fact]
    public async Task RankE_SeesFullBodyAvailable_AndAbcBlocked()
    {
        _trainingProgramRepository
            .Setup(r => r.GetActiveProgramsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingProgram> { FullBody(), Abc() });
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null); // fallback: rank "E"
        _userWorkoutPreferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);

        var result = await CreateHandler().Handle(new GetTrainingProgramsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        var fullBody = result.Single(p => p.ProgramKey == TrainingProgramKeys.FullBody);
        fullBody.IsAvailable.Should().BeTrue();
        fullBody.IsSelected.Should().BeFalse();

        var abc = result.Single(p => p.ProgramKey == TrainingProgramKeys.Abc);
        abc.IsAvailable.Should().BeFalse();
        abc.IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task MarksOnlySelectedProgram_WhenPreferenceMatchesProgramKey()
    {
        _trainingProgramRepository
            .Setup(r => r.GetActiveProgramsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingProgram> { FullBody(), Ab() });
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);
        var preference = UserWorkoutPreference.Create(UserId, "program", TrainingProgramKeys.Ab, UtcNow);
        _userWorkoutPreferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var result = await CreateHandler().Handle(new GetTrainingProgramsQuery(), CancellationToken.None);

        result.Single(p => p.ProgramKey == TrainingProgramKeys.Ab).IsSelected.Should().BeTrue();
        result.Single(p => p.ProgramKey == TrainingProgramKeys.FullBody).IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task DoesNotMarkAsSelected_WhenPreferenceTrainingTypeIsNotProgram()
    {
        _trainingProgramRepository
            .Setup(r => r.GetActiveProgramsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingProgram> { FullBody() });
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);
        var preference = UserWorkoutPreference.Create(UserId, "regeneration", null, UtcNow);
        _userWorkoutPreferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var result = await CreateHandler().Handle(new GetTrainingProgramsQuery(), CancellationToken.None);

        result.Single().IsSelected.Should().BeFalse();
    }
}
