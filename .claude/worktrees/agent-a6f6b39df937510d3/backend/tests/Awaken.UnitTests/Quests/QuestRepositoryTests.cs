using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Quests;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Awaken.UnitTests.Quests;

/// US-238: busca do último Quest concluído do usuário no mesmo programa,
/// base para a rotação cíclica do dia (RN-001/RN-004). Usa o DbContext com
/// provider InMemory (sem Testcontainers), seguindo o mesmo padrão de
/// <see cref="Awaken.UnitTests.Training.TrainingProgramSplitRepositoryTests"/>.
public class QuestRepositoryTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;
    private readonly QuestRepository _repository;

    public QuestRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeService.Object);
        _repository = new QuestRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static Quest BuildCompletedProgramQuest(
        Guid userId, string programKey, DateTime questDateUtc, DateTime completedAtUtc)
    {
        var quest = Quest.Create(userId, questDateUtc, "pt-BR", Guid.NewGuid().ToString());
        quest.ChangeTrainingType("program", programKey, "{}", false, questDateUtc);
        quest.Complete(100, completedAtUtc);
        return quest;
    }

    [Fact]
    public async Task GetLastCompletedByUserAndProgramAsync_ReturnsMostRecent_ForSameUserAndProgram()
    {
        var userId = Guid.NewGuid();
        var older = BuildCompletedProgramQuest(
            userId, "abcde", UtcNow.AddDays(-2), UtcNow.AddDays(-2));
        var newer = BuildCompletedProgramQuest(
            userId, "abcde", UtcNow.AddDays(-1), UtcNow.AddDays(-1));

        _context.Quests.AddRange(older, newer);
        await _context.SaveChangesAsync();

        var result = await _repository.GetLastCompletedByUserAndProgramAsync(userId, "abcde");

        result.Should().NotBeNull();
        result!.Id.Should().Be(newer.Id);
    }

    [Fact]
    public async Task GetLastCompletedByUserAndProgramAsync_IgnoresOtherProgramsAndIncompleteQuests()
    {
        var userId = Guid.NewGuid();
        var completedOtherProgram = BuildCompletedProgramQuest(
            userId, "abc", UtcNow.AddDays(-1), UtcNow.AddDays(-1));

        var incompleteSameProgram = Quest.Create(userId, UtcNow, "pt-BR", Guid.NewGuid().ToString());
        incompleteSameProgram.ChangeTrainingType("program", "abcde", "{}", false, UtcNow);
        // não concluída: sem Complete().

        _context.Quests.AddRange(completedOtherProgram, incompleteSameProgram);
        await _context.SaveChangesAsync();

        var result = await _repository.GetLastCompletedByUserAndProgramAsync(userId, "abcde");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLastCompletedByUserAndProgramAsync_ReturnsNull_WhenNoHistory()
    {
        var result = await _repository.GetLastCompletedByUserAndProgramAsync(Guid.NewGuid(), "abcde");

        result.Should().BeNull();
    }
}
