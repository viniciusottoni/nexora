using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.GetResolvedProgramDay;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Quests;

/// US-238 §18: endpoint de auditoria que expõe o dia do programa resolvido
/// pela rotação cíclica, a partir da preferência ativa do usuário
/// (US-231/232), do split map (US-237) e do último Quest concluído (US-062).
public class GetResolvedProgramDayQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserWorkoutPreferenceRepository> _userWorkoutPreferenceRepository = new();
    private readonly Mock<ITrainingProgramSplitRepository> _trainingProgramSplitRepository = new();
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ILogger<GetResolvedProgramDayQueryHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    public GetResolvedProgramDayQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetResolvedProgramDayQueryHandler CreateHandler() => new(
        _currentUserService.Object,
        _userWorkoutPreferenceRepository.Object,
        _trainingProgramSplitRepository.Object,
        _questRepository.Object,
        _logger.Object);

    private static TrainingSplitDaySeed Day(string dayKey) => new(
        dayKey, $"programDay{dayKey}", "role",
        [MuscleGroups.Chest], [], [MovementPatterns.HorizontalPush],
        true, 4, 6);

    private static Quest BuildCompletedQuest(string programKey, string resolvedDayKey)
    {
        var quest = Quest.Create(UserId, UtcNow.Date, "pt-BR", Guid.NewGuid().ToString());
        quest.ChangeTrainingType("program", programKey, "{}", false, UtcNow);
        quest.AssignResolvedProgramDay(programKey, resolvedDayKey, 3, "v1", UtcNow);
        quest.Complete(100, UtcNow);
        return quest;
    }

    private void SetupPreference(string? preferredProgramId) =>
        _userWorkoutPreferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferredProgramId is null
                ? null
                : UserWorkoutPreference.Create(UserId, "program", preferredProgramId, UtcNow));

    // Cenário direto do exemplo do §18 da US-238.
    [Fact]
    public async Task Handle_ReturnsCyclicSuccessor_ForAbcdeLastCompletedC()
    {
        SetupPreference(TrainingProgramKeys.Abcde);

        var split = TrainingProgramSplit.Create(
            TrainingProgramKeys.Abcde, "v1", [Day("A"), Day("B"), Day("C"), Day("D"), Day("E")]);
        _trainingProgramSplitRepository
            .Setup(r => r.GetByProgramKeyAsync(TrainingProgramKeys.Abcde, It.IsAny<CancellationToken>()))
            .ReturnsAsync(split);

        var lastCompleted = BuildCompletedQuest(TrainingProgramKeys.Abcde, "C");
        _questRepository
            .Setup(r => r.GetLastCompletedByUserAndProgramAsync(
                UserId, TrainingProgramKeys.Abcde, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastCompleted);

        var result = await CreateHandler().Handle(new GetResolvedProgramDayQuery(), CancellationToken.None);

        result.ProgramKey.Should().Be("abcde");
        result.LastCompletedDayKey.Should().Be("C");
        result.ResolvedDayKey.Should().Be("D");
        result.ResolvedDayIndex.Should().Be(4);
        result.SplitMapVersion.Should().Be("v1");
        result.Reason.Should().Be("cyclic_successor");
    }

    [Fact]
    public async Task Handle_ReturnsFirstWorkout_WhenNoHistory()
    {
        SetupPreference(TrainingProgramKeys.Abc);

        var split = TrainingProgramSplit.Create(
            TrainingProgramKeys.Abc, "v1", [Day("A"), Day("B"), Day("C")]);
        _trainingProgramSplitRepository
            .Setup(r => r.GetByProgramKeyAsync(TrainingProgramKeys.Abc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(split);

        _questRepository
            .Setup(r => r.GetLastCompletedByUserAndProgramAsync(
                UserId, TrainingProgramKeys.Abc, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        var result = await CreateHandler().Handle(new GetResolvedProgramDayQuery(), CancellationToken.None);

        result.LastCompletedDayKey.Should().BeNull();
        result.ResolvedDayKey.Should().Be("A");
        result.ResolvedDayIndex.Should().Be(1);
        result.Reason.Should().Be("first_workout");
    }

    [Fact]
    public async Task Handle_Throws_WhenUserHasNoProgramSelected()
    {
        SetupPreference(null);

        var act = () => CreateHandler().Handle(new GetResolvedProgramDayQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
