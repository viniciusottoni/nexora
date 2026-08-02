using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Hunter.Commands.EquipCosmetic;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Hunter;

public class EquipCosmeticCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

    public EquipCosmeticCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private EquipCosmeticCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _dateTimeService.Object,
        _hunterProgressionRepository.Object,
        _inventoryRepository.Object,
        _unitOfWork.Object);

    private HunterProgression SetupExistingProgression()
    {
        var progression = HunterProgression.Create(UserId);
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        return progression;
    }

    // ─── Sucesso: equipar moldura possuída ─────────────────────────────────────

    [Fact]
    public async Task Succeeds_WhenEquippingOwnedFrame()
    {
        var progression = SetupExistingProgression();
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.FrameRankB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.FrameRankB, quantity: 1));

        await CreateHandler().Handle(
            new EquipCosmeticCommand("frame", ItemKeys.FrameRankB), CancellationToken.None);

        progression.EquippedFrameKey.Should().Be(ItemKeys.FrameRankB);
        progression.UpdatedAtUtc.Should().Be(UtcNow);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Bloqueia: item não possuído ───────────────────────────────────────────

    [Fact]
    public async Task Throws_WhenItemNotOwned()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.FrameRankB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var act = () => CreateHandler().Handle(
            new EquipCosmeticCommand("frame", ItemKeys.FrameRankB), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("COSMETIC_NOT_OWNED");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Throws_WhenItemOwnedWithZeroQuantity()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.FrameRankB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.FrameRankB, quantity: 0));

        var act = () => CreateHandler().Handle(
            new EquipCosmeticCommand("frame", ItemKeys.FrameRankB), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("COSMETIC_NOT_OWNED");
    }

    // ─── Bloqueia: categoria não bate com o slot ───────────────────────────────

    [Fact]
    public async Task Throws_WhenItemCategoryDoesNotMatchSlot()
    {
        var act = () => CreateHandler().Handle(
            new EquipCosmeticCommand("frame", ItemKeys.AuraDefault), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("COSMETIC_CATEGORY_MISMATCH");

        _inventoryRepository.Verify(
            r => r.GetByUserIdAndItemKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Sucesso: remove cosmético equipado quando ItemKey=null ────────────────

    [Fact]
    public async Task Succeeds_WhenUnequippingFrame()
    {
        var progression = SetupExistingProgression();
        progression.EquipFrame(ItemKeys.FrameRankB, UtcNow.AddDays(-1));

        await CreateHandler().Handle(
            new EquipCosmeticCommand("frame", null), CancellationToken.None);

        progression.EquippedFrameKey.Should().BeNull();
        _inventoryRepository.Verify(
            r => r.GetByUserIdAndItemKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── HunterProgression inexistente ──────────────────────────────────────────

    [Fact]
    public async Task Throws_WhenHunterProgressionNotFound()
    {
        _hunterProgressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.AuraDefault, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.AuraDefault, quantity: 1));

        var act = () => CreateHandler().Handle(
            new EquipCosmeticCommand("aura", ItemKeys.AuraDefault), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
