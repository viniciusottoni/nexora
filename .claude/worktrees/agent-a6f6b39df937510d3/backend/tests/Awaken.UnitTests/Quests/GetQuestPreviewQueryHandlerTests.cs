using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.GetQuestPreview;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class GetQuestPreviewQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid QuestId = Guid.NewGuid();

    private const string WorkoutJson = """
    {
      "title": "Daily Quest",
      "description": "Full body",
      "durationMinutes": 30,
      "exercises": [
        { "name": "Squat", "sets": 3, "repsMin": 10, "repsMax": 15, "restSeconds": 90, "targetRpe": "6-8" }
      ]
    }
    """;

    public GetQuestPreviewQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetQuestPreviewQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object);

    private Quest BuildPendingPersonalizedQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow, isPersonalized: true);
        return quest;
    }

    private Quest BuildFallbackPendingQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow, isPersonalized: false);
        return quest;
    }

    [Fact]
    public async Task CA001_ReturnsPreview_WhenQuestBelongsToUserAndIsPending()
    {
        var quest = BuildPendingPersonalizedQuest();
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.QuestId.Should().Be(quest.Id);
        result.QuestType.Should().Be("daily");
        result.TrainingType.Should().Be("personalized_individual");
        result.CanChangeTrainingType.Should().BeTrue();
        result.EstimatedDurationMinutes.Should().Be(30);
        result.EstimatedXp.Should().Be(120); // 30 * 4
        result.Workout.Should().NotBeNull();
        result.Workout!.Exercises.Should().HaveCount(1);
        result.Workout.Exercises.First().Name.Should().Be("Squat");
    }

    [Fact]
    public async Task CA001_CanChangeTrainingType_IsFalse_WhenQuestIsInProgress()
    {
        var quest = BuildPendingPersonalizedQuest();
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.QuestType.Should().Be("daily");
        result.CanChangeTrainingType.Should().BeFalse();
    }

    [Fact]
    public async Task CA001_CanChangeTrainingType_IsFalse_WhenQuestIsCompleted()
    {
        var quest = BuildPendingPersonalizedQuest();
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        quest.Complete(xpAwarded: 120, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.QuestType.Should().Be("daily");
        result.CanChangeTrainingType.Should().BeFalse();
    }

    [Fact]
    public async Task CA001_TrainingType_IsFallback_WhenQuestIsNotPersonalized()
    {
        var quest = BuildFallbackPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.QuestType.Should().Be("daily");
        result.TrainingType.Should().Be("fallback");
    }

    [Fact]
    public async Task RN001_Throws_NotFoundException_WhenQuestDoesNotExist()
    {
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        await CreateHandler()
            .Invoking(h => h.Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RN001_Throws_UnauthorizedException_WhenQuestBelongsToAnotherUser()
    {
        var otherUserId = Guid.NewGuid();
        var quest = Quest.Create(otherUserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow, isPersonalized: true);
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}



