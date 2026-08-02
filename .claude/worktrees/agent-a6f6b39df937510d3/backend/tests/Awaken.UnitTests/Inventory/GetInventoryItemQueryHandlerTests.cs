using Awaken.Application.Common.Interfaces;
using Awaken.Application.Inventory.Queries.GetInventoryItem;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Inventory;

public class GetInventoryItemQueryHandlerTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetInventoryItemQueryHandlerTests() =>
        _currentUserService.Setup(s => s.UserId).Returns(UserId);

    private GetInventoryItemQueryHandler CreateHandler() =>
        new(_inventoryRepository.Object, _currentUserService.Object);

    [Fact]
    public async Task ReturnsQuantity_WhenItemExists()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 3);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await CreateHandler().Handle(
            new GetInventoryItemQuery(ItemKeys.ReforgeScroll), CancellationToken.None);

        result.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task ReturnsZero_WhenItemNeverGranted()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(
            new GetInventoryItemQuery(ItemKeys.ReforgeScroll), CancellationToken.None);

        result.Quantity.Should().Be(0);
    }
}
