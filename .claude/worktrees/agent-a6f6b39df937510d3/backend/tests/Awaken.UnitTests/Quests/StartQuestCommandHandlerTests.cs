using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.StartQuest;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Quests;

public class StartQuestCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<StartQuestCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 22, 9, 0, 0, DateTimeKind.Utc);

    public StartQuestCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private StartQuestCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static Quest BuildQuest(Guid userId, string type = "daily")
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");

        // Type ainda nao tem fabrica publica para dungeon/raid (fora do escopo da US-056);
        // usamos reflection apenas para exercitar a validacao do handler sobre o campo Type.
        if (type != "daily")
        {
            typeof(Quest).GetProperty(nameof(Quest.Type))!.SetValue(quest, type);
        }

        return quest;
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("dungeon")]
    [InlineData("raid")]
    public async Task CA001_StartsPendingQuest_AndPersists(string type)
    {
        var quest = BuildQuest(UserId, type);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("in_progress");
        result.StartedAtUtc.Should().Be(UtcNow);
        result.QuestType.Should().Be(type);
        quest.Status.Should().Be("in_progress");
        quest.StartedAtUtc.Should().Be(UtcNow);
        _questRepository.Verify(r => r.AddExercisesAsync(quest.Exercises, It.IsAny<CancellationToken>()), Times.Once);
        _questRepository.Verify(r => r.UpdateRoot(quest), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("quest_started") &&
                    v.ToString()!.Contains($"userId={UserId}") &&
                    v.ToString()!.Contains($"questType={type}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RN003_StartingAlreadyInProgressQuest_IsIdempotent_DoesNotRewriteStartedAt()
    {
        var quest = BuildQuest(UserId);
        quest.Start(UtcNow, Array.Empty<QuestExerciseSeed>());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow.AddHours(1));

        var result = await CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("in_progress");
        result.StartedAtUtc.Should().Be(UtcNow);
        quest.StartedAtUtc.Should().Be(UtcNow);
        _questRepository.Verify(r => r.UpdateRoot(It.IsAny<Quest>()), Times.Never);
        _questRepository.Verify(r => r.AddExercisesAsync(It.IsAny<IEnumerable<QuestExercise>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("quest_started")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdAsync(questId, It.IsAny<CancellationToken>())).ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new StartQuestCommand(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildQuest(Guid.NewGuid());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsConflict_WhenQuestTypeIsInvalid()
    {
        var quest = BuildQuest(UserId, "invalid_type");
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("INVALID_QUEST_TYPE");
    }

    [Fact]
    public async Task ThrowsConflict_WhenQuestIsCompleted()
    {
        var quest = BuildQuest(UserId);
        quest.Complete(0, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_STARTABLE");
    }

    // ── US-057: materializacao de QuestExercise a partir do WorkoutJson ───────

    [Fact]
    public async Task Start_MaterializesExercises_OrderedFromWorkoutJson()
    {
        var quest = BuildQuest(UserId);
        quest.AssignWorkout("""
            {
              "title": "Daily Quest",
              "exercises": [
                { "id": "ex-1", "name": "Squat", "sets": 3, "repsMin": 8, "repsMax": 12, "restSeconds": 60, "targetRpe": "8",
                  "attributeContribution": { "strengthXp": 10, "agilityXp": 0, "enduranceXp": 0, "vitalityXp": 0, "focusXp": 0, "wisdomXp": 1 } },
                { "name": "Push-up", "sets": 3, "repsMin": 10, "repsMax": 15, "restSeconds": 45 }
              ]
            }
            """, UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        await CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        quest.Exercises.Should().HaveCount(2);
        quest.Exercises[0].Order.Should().Be(1);
        quest.Exercises[0].Name.Should().Be("Squat");
        quest.Exercises[0].ExerciseCatalogProviderId.Should().Be("ex-1");
        quest.Exercises[1].Order.Should().Be(2);
        quest.Exercises[1].Name.Should().Be("Push-up");
        quest.Exercises[1].ExerciseCatalogProviderId.Should().BeNull();
    }

    [Fact]
    public async Task Start_CalledTwice_DoesNotDuplicateExercises()
    {
        var quest = BuildQuest(UserId);
        quest.AssignWorkout("""
            {
              "exercises": [
                { "name": "Squat", "sets": 3, "repsMin": 8 }
              ]
            }
            """, UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        await CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);
        await CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        quest.Exercises.Should().HaveCount(1);
    }

    // ── US-139: rastrear ativacao de dungeon ──────────────────────────────────

    [Fact]
    public async Task US139_LogsDungeonActivated_WhenDungeonQuestStarted()
    {
        var quest = BuildQuest(UserId, "dungeon");
        quest.AssignWorkout("""
            {
              "exercises": [
                { "name": "Burpee", "sets": 3, "repsMin": 10 }
              ]
            }
            """, UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        await CreateHandler().Handle(new StartQuestCommand(quest.Id), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dungeon_activated") &&
                    v.ToString()!.Contains($"userId={UserId}") &&
                    v.ToString()!.Contains("questType=dungeon") &&
                    v.ToString()!.Contains("source=quest_start") &&
                    v.ToString()!.Contains("status=activated") &&
                    v.ToString()!.Contains("result=success")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
