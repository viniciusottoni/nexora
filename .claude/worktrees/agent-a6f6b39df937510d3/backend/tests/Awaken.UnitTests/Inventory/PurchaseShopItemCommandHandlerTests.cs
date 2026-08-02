using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Commands.PurchaseShopItem;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Inventory;

public class PurchaseShopItemCommandHandlerTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IShopProductRepository> _shopProductRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc);

    public PurchaseShopItemCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        SetupProduct(ItemKeys.ReforgeScroll, priceGold: 150);
        SetupProduct(ItemKeys.DungeonStone, priceGold: 120);
        _shopProductRepository
            .Setup(r => r.GetByKeyAsync("unknown_item", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopProduct?)null);
    }

    private void SetupProduct(string itemKey, int priceGold)
    {
        var product = ShopProduct.Create(
            itemKey, itemKey, null, "consumable", "common", revenueCatProductId: null, UtcNow, priceGold);
        _shopProductRepository
            .Setup(r => r.GetByKeyAsync(itemKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    private PurchaseShopItemCommandHandler CreateHandler() =>
        new(
            _inventoryRepository.Object,
            _shopProductRepository.Object,
            _currentUserService.Object,
            _dateTimeService.Object,
            _unitOfWork.Object
        );

    [Fact]
    public async Task CreatesInventoryItem_WithOneUnit_WhenUserNeverOwnedIt()
    {
        _inventoryRepository
            .Setup(r =>
                r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(
            new PurchaseShopItemCommand(ItemKeys.ReforgeScroll),
            CancellationToken.None
        );

        result.Quantity.Should().Be(1);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddsOneUnit_WhenUserAlreadyOwnsIt()
    {
        var existing = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 1);
        _inventoryRepository
            .Setup(r =>
                r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(
            new PurchaseShopItemCommand(ItemKeys.ReforgeScroll),
            CancellationToken.None
        );

        result.Quantity.Should().Be(2);
        _inventoryRepository.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task CreatesInventoryItem_ForDungeonStone_WhenUserNeverOwnedIt()
    {
        _inventoryRepository
            .Setup(r =>
                r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(
            new PurchaseShopItemCommand(ItemKeys.DungeonStone),
            CancellationToken.None
        );

        result.Quantity.Should().Be(1);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenItemKeyIsNotInCatalog()
    {
        var act = () =>
            CreateHandler().Handle(new PurchaseShopItemCommand("unknown_item"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
