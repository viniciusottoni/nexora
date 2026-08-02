using Awaken.Domain.Entities.Training;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Awaken.Application.Common.Interfaces;

namespace Awaken.UnitTests.Training;

/// US-237 (Task 2.3): confirma que o split map persistido é lido de volta com
/// os dias na ordem certa (DayIndex asc), via repositório real contra o
/// DbContext (InMemory provider — sem depender de Testcontainers, seguindo o
/// mesmo padrão de <see cref="Awaken.UnitTests.Admin.Security.SecurityAlertRepositoryTests"/>).
public class TrainingProgramSplitRepositoryTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;
    private readonly TrainingProgramSplitRepository _repository;

    public TrainingProgramSplitRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeService.Object);
        _repository = new TrainingProgramSplitRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static TrainingSplitDaySeed Day(string dayKey, string role, string labelI18nKey) => new(
        dayKey, labelI18nKey, role,
        [MuscleGroups.Chest], [], [MovementPatterns.HorizontalPush],
        true, 4, 6);

    [Fact]
    public async Task GetByProgramKeyAsync_ReturnsDaysInOrder_ForAbc()
    {
        var split = TrainingProgramSplit.Create(
            TrainingProgramKeys.Abc,
            "v1",
            [
                Day("A", "push", "programDayPush"),
                Day("B", "pull", "programDayPull"),
                Day("C", "legs", "programDayLegs"),
            ]);

        _context.TrainingProgramSplits.Add(split);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByProgramKeyAsync(TrainingProgramKeys.Abc);

        result.Should().NotBeNull();
        result!.ProgramKey.Should().Be(TrainingProgramKeys.Abc);
        result.Days.Select(d => d.DayKey).Should().Equal("A", "B", "C");
        result.Days.Select(d => d.DayIndex).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task GetByProgramKeyAsync_ReturnsNull_WhenProgramHasNoSplit()
    {
        var result = await _repository.GetByProgramKeyAsync(TrainingProgramKeys.Perfect2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProgramKeyAsync_ReturnsNull_WhenSplitIsInactive()
    {
        var split = TrainingProgramSplit.Create(
            TrainingProgramKeys.FullBody, "v1", [Day("FB", "full_body", "programDayFullBody")]);
        _context.TrainingProgramSplits.Add(split);
        await _context.SaveChangesAsync();

        // Simula uma versão desativada diretamente no banco (sem setter público de IsActive
        // hoje na entidade — este teste documenta o filtro do repositório caso uma futura
        // versão do split seja desativada).
        _context.Entry(split).Property("IsActive").CurrentValue = false;
        await _context.SaveChangesAsync();

        var result = await _repository.GetByProgramKeyAsync(TrainingProgramKeys.FullBody);

        result.Should().BeNull();
    }
}
