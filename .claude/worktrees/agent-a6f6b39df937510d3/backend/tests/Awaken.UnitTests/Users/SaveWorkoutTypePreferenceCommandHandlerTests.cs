using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Commands.SaveWorkoutTypePreference;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Users;

public class SaveWorkoutTypePreferenceCommandHandlerTests
{
    private readonly Mock<IUserWorkoutPreferenceRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);

    public SaveWorkoutTypePreferenceCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(Now);
    }

    private SaveWorkoutTypePreferenceCommandHandler CreateHandler() => new(
        _repository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    // ── CA-001: cria preferencia quando nao existe ────────────────────────────

    [Fact]
    public async Task CA001_Creates_WhenNoExistingPreference()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);

        UserWorkoutPreference? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserWorkoutPreference, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new SaveWorkoutTypePreferenceCommand("program", "perfect_2"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.UserId.Should().Be(UserId);
        added.PreferredTrainingType.Should().Be("program");
        added.PreferredProgramId.Should().Be("perfect_2");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Upsert: atualiza preferencia existente ────────────────────────────────

    [Fact]
    public async Task Updates_WhenPreferenceAlreadyExists()
    {
        var existing = UserWorkoutPreference.Create(UserId, "regeneration", null, Now);
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(
            new SaveWorkoutTypePreferenceCommand("program", "saitama_path"), CancellationToken.None);

        existing.PreferredTrainingType.Should().Be("program");
        existing.PreferredProgramId.Should().Be("saitama_path");
        _repository.Verify(r => r.Update(existing), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RN-002/RN-003: programId descartado para tipos nao-programa ───────────

    [Fact]
    public async Task DropsProgramId_WhenTypeIsNotProgram()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);

        UserWorkoutPreference? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserWorkoutPreference, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new SaveWorkoutTypePreferenceCommand("regeneration", "saitama_path"), CancellationToken.None);

        added!.PreferredProgramId.Should().BeNull();
    }
}
