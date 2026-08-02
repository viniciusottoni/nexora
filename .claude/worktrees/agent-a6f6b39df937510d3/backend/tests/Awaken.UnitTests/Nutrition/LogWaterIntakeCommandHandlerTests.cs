using Awaken.Application.Common.Interfaces;
using Awaken.Application.Nutrition.Commands.LogWaterIntake;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Nutrition;

public class LogWaterIntakeCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 28);

    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<INutritionLogRepository> _nutritionLogRepository = new();
    private readonly Mock<IUserDateService> _userDateService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public LogWaterIntakeCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _userDateService.Setup(s => s.TodayLocal).Returns(Today);
        _unitOfWork.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private LogWaterIntakeCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _nutritionLogRepository.Object,
        _userDateService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task CreatesNewLogUsingLocalDate_WhenLogDoesNotExist()
    {
        NutritionLog? capturedLog = null;
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NutritionLog?)null);
        _nutritionLogRepository
            .Setup(r => r.AddAsync(It.IsAny<NutritionLog>(), It.IsAny<CancellationToken>()))
            .Callback<NutritionLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(new LogWaterIntakeCommand(250), CancellationToken.None);

        result.WaterConsumedMl.Should().Be(250);
        capturedLog.Should().NotBeNull();
        capturedLog!.Date.Should().Be(Today);
        capturedLog.WaterMl.Should().Be(250);
        _unitOfWork.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddsWaterToExistingLog()
    {
        var existingLog = NutritionLog.Create(UserId, Today);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);

        var result = await CreateHandler().Handle(new LogWaterIntakeCommand(300), CancellationToken.None);

        result.WaterConsumedMl.Should().Be(300);
        existingLog.WaterMl.Should().Be(300);
        _nutritionLogRepository.Verify(
            r => r.AddAsync(It.IsAny<NutritionLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
