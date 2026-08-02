using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.ConfirmDailyQuest;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class ConfirmDailyQuestCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 22, 9, 0, 0, DateTimeKind.Utc);

    public ConfirmDailyQuestCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private ConfirmDailyQuestCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static Quest BuildQuest(Guid userId) =>
        Quest.Create(userId, new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");

    [Fact]
    public async Task CA004_ConfirmsQuest_AndPersists()
    {
        var quest = BuildQuest(UserId);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new ConfirmDailyQuestCommand(quest.Id), CancellationToken.None);

        result.IsConfirmed.Should().BeTrue();
        quest.IsConfirmed.Should().BeTrue();
        quest.ConfirmedAtUtc.Should().Be(UtcNow);
        _questRepository.Verify(r => r.Update(quest), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RN008_ConfirmingTwiceIsIdempotent_DoesNotChangeConfirmedAt()
    {
        var quest = BuildQuest(UserId);
        quest.Confirm(UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow.AddHours(1));

        var result = await CreateHandler().Handle(new ConfirmDailyQuestCommand(quest.Id), CancellationToken.None);

        result.IsConfirmed.Should().BeTrue();
        quest.ConfirmedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdAsync(questId, It.IsAny<CancellationToken>())).ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new ConfirmDailyQuestCommand(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsUnauthorized_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildQuest(Guid.NewGuid());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new ConfirmDailyQuestCommand(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
