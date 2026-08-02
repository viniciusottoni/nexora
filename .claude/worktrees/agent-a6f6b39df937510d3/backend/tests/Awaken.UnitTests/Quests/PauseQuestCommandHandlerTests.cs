using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.PauseQuest;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class PauseQuestCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc);

    public PauseQuestCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private PauseQuestCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static Quest BuildInProgressQuest(Guid userId)
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        quest.Start(UtcNow, Array.Empty<QuestExerciseSeed>());
        return quest;
    }

    [Fact]
    public async Task CA001_PausesInProgressQuest_AndPersists()
    {
        var quest = BuildInProgressQuest(UserId);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new PauseQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("paused");
        result.PausedAtUtc.Should().Be(UtcNow);
        quest.Status.Should().Be("paused");
        _questRepository.Verify(r => r.Update(quest), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RN003_PausingAlreadyPausedQuest_IsIdempotent_DoesNotRewritePausedAt()
    {
        var quest = BuildInProgressQuest(UserId);
        quest.Pause(UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow.AddHours(1));

        var result = await CreateHandler().Handle(new PauseQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("paused");
        result.PausedAtUtc.Should().Be(UtcNow);
        _questRepository.Verify(r => r.Update(It.IsAny<Quest>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdAsync(questId, It.IsAny<CancellationToken>())).ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new PauseQuestCommand(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildInProgressQuest(Guid.NewGuid());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new PauseQuestCommand(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RN006_ThrowsConflict_WhenQuestIsPending()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new PauseQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_PAUSABLE");
    }

    [Fact]
    public async Task RN006_ThrowsConflict_WhenQuestIsCompleted()
    {
        var quest = BuildInProgressQuest(UserId);
        quest.Complete(0, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new PauseQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_PAUSABLE");
    }

    [Fact]
    public async Task RN006_ThrowsConflict_WhenQuestIsCancelled()
    {
        var quest = BuildInProgressQuest(UserId);
        quest.Cancel(UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new PauseQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_PAUSABLE");
    }
}
