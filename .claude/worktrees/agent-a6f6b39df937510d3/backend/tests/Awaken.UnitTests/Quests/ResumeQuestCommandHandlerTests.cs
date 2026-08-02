using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.ResumeQuest;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class ResumeQuestCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc);

    public ResumeQuestCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private ResumeQuestCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static Quest BuildPausedQuest(Guid userId)
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        quest.Start(UtcNow, Array.Empty<QuestExerciseSeed>());
        quest.Pause(UtcNow);
        return quest;
    }

    [Fact]
    public async Task CA002_ResumesPausedQuest_AndPersists()
    {
        var quest = BuildPausedQuest(UserId);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new ResumeQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("in_progress");
        result.ResumedAtUtc.Should().Be(UtcNow);
        quest.Status.Should().Be("in_progress");
        _questRepository.Verify(r => r.Update(quest), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RN_ResumingAlreadyInProgressQuest_IsIdempotent_DoesNotRewriteResumedAt()
    {
        var quest = BuildPausedQuest(UserId);
        quest.Resume(UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow.AddHours(1));

        var result = await CreateHandler().Handle(new ResumeQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("in_progress");
        result.ResumedAtUtc.Should().Be(UtcNow);
        _questRepository.Verify(r => r.Update(It.IsAny<Quest>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdAsync(questId, It.IsAny<CancellationToken>())).ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new ResumeQuestCommand(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildPausedQuest(Guid.NewGuid());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new ResumeQuestCommand(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RN002_ThrowsConflict_WhenQuestIsPending()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new ResumeQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_RESUMABLE");
    }

    [Fact]
    public async Task RN002_ThrowsConflict_WhenQuestIsCancelled()
    {
        var quest = BuildPausedQuest(UserId);
        quest.Cancel(UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new ResumeQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_RESUMABLE");
    }
}
