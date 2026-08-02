using Awaken.Application.Common.Interfaces;
using Awaken.Application.Nutrition.Commands.UpdateCupVolume;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Nutrition;

public class UpdateCupVolumeCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserNutritionPreferenceRepository> _preferenceRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateCupVolumeCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private UpdateCupVolumeCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _preferenceRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task CreatesPreferenceWhenNoneExists()
    {
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNutritionPreference?)null);

        UserNutritionPreference? added = null;
        _preferenceRepository
            .Setup(r => r.AddAsync(It.IsAny<UserNutritionPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserNutritionPreference, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(new UpdateCupVolumeCommand(300), CancellationToken.None);

        result.CupVolumeMl.Should().Be(300);
        added.Should().NotBeNull();
        added!.CupVolumeMl.Should().Be(300);
        added.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task UpdatesExistingPreference()
    {
        var existing = UserNutritionPreference.Create(UserId, 250);
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(new UpdateCupVolumeCommand(500), CancellationToken.None);

        result.CupVolumeMl.Should().Be(500);
        existing.CupVolumeMl.Should().Be(500);
        _preferenceRepository.Verify(r => r.AddAsync(It.IsAny<UserNutritionPreference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SavesChangesAfterUpdate()
    {
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNutritionPreference?)null);
        _preferenceRepository
            .Setup(r => r.AddAsync(It.IsAny<UserNutritionPreference>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(new UpdateCupVolumeCommand(250), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
