using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.TrainingPrograms.Commands.SelectTrainingProgram;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.TrainingPrograms;

public class SelectTrainingProgramCommandHandlerTests
{
    private readonly Mock<ITrainingProgramRepository> _trainingProgramRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IUserWorkoutPreferenceRepository> _userWorkoutPreferenceRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);

    public SelectTrainingProgramCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private SelectTrainingProgramCommandHandler CreateHandler() => new(
        _trainingProgramRepository.Object,
        _hunterProgressionRepository.Object,
        _userWorkoutPreferenceRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static TrainingProgram FullBody() => TrainingProgram.Create(
        TrainingProgramKeys.FullBody, "Full Body", "Corpo inteiro em cada sessao.", "Sedentario", "E+", 1, UtcNow);

    private static TrainingProgram Abc() => TrainingProgram.Create(
        TrainingProgramKeys.Abc, "ABC", "Classico das academias.", "Intermediario", "C+", 3, UtcNow);

    [Fact]
    public async Task Succeeds_WhenRankSufficient_AndPreferenceDoesNotExistYet()
    {
        _trainingProgramRepository
            .Setup(r => r.GetByKeyAsync(TrainingProgramKeys.FullBody, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FullBody());
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null); // fallback: rank "E"
        _userWorkoutPreferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);

        UserWorkoutPreference? added = null;
        _userWorkoutPreferenceRepository
            .Setup(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserWorkoutPreference, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new SelectTrainingProgramCommand(TrainingProgramKeys.FullBody), CancellationToken.None);

        added.Should().NotBeNull();
        added!.UserId.Should().Be(UserId);
        added.PreferredTrainingType.Should().Be("program");
        added.PreferredProgramId.Should().Be(TrainingProgramKeys.FullBody);
        _userWorkoutPreferenceRepository.Verify(
            r => r.Update(It.IsAny<UserWorkoutPreference>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Succeeds_WhenPreferenceAlreadyExists_UpdatesInstead()
    {
        _trainingProgramRepository
            .Setup(r => r.GetByKeyAsync(TrainingProgramKeys.FullBody, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FullBody());
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);
        var existing = UserWorkoutPreference.Create(UserId, "regeneration", null, UtcNow);
        _userWorkoutPreferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(
            new SelectTrainingProgramCommand(TrainingProgramKeys.FullBody), CancellationToken.None);

        existing.PreferredTrainingType.Should().Be("program");
        existing.PreferredProgramId.Should().Be(TrainingProgramKeys.FullBody);
        _userWorkoutPreferenceRepository.Verify(r => r.Update(existing), Times.Once);
        _userWorkoutPreferenceRepository.Verify(
            r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrowsConflict_WhenRankInsufficient_AndDoesNotPersistAnything()
    {
        _trainingProgramRepository
            .Setup(r => r.GetByKeyAsync(TrainingProgramKeys.Abc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Abc());
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null); // fallback: rank "E", programa exige "C"

        var act = () => CreateHandler().Handle(
            new SelectTrainingProgramCommand(TrainingProgramKeys.Abc), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("RANK_REQUIREMENT_NOT_MET");

        _userWorkoutPreferenceRepository.Verify(
            r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _userWorkoutPreferenceRepository.Verify(r => r.Update(It.IsAny<UserWorkoutPreference>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenProgramKeyDoesNotExistInCatalog()
    {
        _trainingProgramRepository
            .Setup(r => r.GetByKeyAsync("unknown_program", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingProgram?)null);

        var act = () => CreateHandler().Handle(
            new SelectTrainingProgramCommand("unknown_program"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
