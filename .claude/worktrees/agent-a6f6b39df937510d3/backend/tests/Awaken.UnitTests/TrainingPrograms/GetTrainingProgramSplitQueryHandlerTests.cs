using Awaken.Application.Common.Exceptions;
using Awaken.Application.TrainingPrograms.Queries.GetTrainingProgramSplit;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.TrainingPrograms;

public class GetTrainingProgramSplitQueryHandlerTests
{
    private readonly Mock<ITrainingProgramSplitRepository> _trainingProgramSplitRepository = new();

    private GetTrainingProgramSplitQueryHandler CreateHandler() => new(_trainingProgramSplitRepository.Object);

    private static TrainingSplitDaySeed Day(
        string dayKey, string role, string labelI18nKey,
        IReadOnlyList<string> targetMuscleGroups, IReadOnlyList<string> targetMovementPatterns,
        bool allowsCoreFinisher) => new(
            dayKey, labelI18nKey, role, targetMuscleGroups, [], targetMovementPatterns, allowsCoreFinisher, 4, 6);

    private static TrainingProgramSplit AbcSplit() => TrainingProgramSplit.Create(
        TrainingProgramKeys.Abc,
        "v1",
        [
            Day("A", "push", "programDayPush",
                [MuscleGroups.Chest, MuscleGroups.Shoulders, MuscleGroups.Triceps],
                [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.CoreFlexion],
                allowsCoreFinisher: true),
            Day("B", "pull", "programDayPull",
                [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts, MuscleGroups.Traps],
                [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull],
                allowsCoreFinisher: false),
            Day("C", "legs", "programDayLegs",
                [MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Calves, MuscleGroups.Core],
                [MovementPatterns.Squat, MovementPatterns.Hinge, MovementPatterns.Lunge, MovementPatterns.CoreFlexion],
                allowsCoreFinisher: true),
        ]);

    [Fact]
    public async Task Handle_ReturnsDaysInOrder_WhenProgramHasSplit()
    {
        _trainingProgramSplitRepository
            .Setup(r => r.GetByProgramKeyAsync(TrainingProgramKeys.Abc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AbcSplit());

        var result = await CreateHandler().Handle(
            new GetTrainingProgramSplitQuery(TrainingProgramKeys.Abc), CancellationToken.None);

        result.ProgramKey.Should().Be(TrainingProgramKeys.Abc);
        result.SplitMapVersion.Should().Be("v1");
        result.Days.Should().HaveCount(3);
        result.Days.Select(d => d.DayKey).Should().Equal("A", "B", "C");
        result.Days[0].Role.Should().Be("push");
        result.Days[0].LabelI18nKey.Should().Be("programDayPush");
        result.Days[0].TargetMuscleGroups.Should().Contain(MuscleGroups.Chest);
        result.Days[0].TargetMovementPatterns.Should().Contain(MovementPatterns.HorizontalPush);
        result.Days[0].AllowsCoreFinisher.Should().BeTrue();
        result.Days[1].AllowsCoreFinisher.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Throws_WhenProgramHasNoSplit()
    {
        _trainingProgramSplitRepository
            .Setup(r => r.GetByProgramKeyAsync(TrainingProgramKeys.Perfect2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingProgramSplit?)null);

        var act = () => CreateHandler().Handle(
            new GetTrainingProgramSplitQuery(TrainingProgramKeys.Perfect2), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
