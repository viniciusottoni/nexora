using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Awaken.UnitTests.Inventory;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    public InventoryServiceTests() => _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

    private InventoryService CreateService() =>
        new(_inventoryRepository.Object, _dateTimeService.Object, _unitOfWork.Object);

    // CA-001: item nunca obtido deve retornar quantidade 0 sem erro.
    [Fact]
    public async Task GetQuantityAsync_ReturnsZero_WhenItemNeverGranted()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var quantity = await CreateService().GetQuantityAsync(UserId, ItemKeys.ReforgeScroll);

        quantity.Should().Be(0);
    }

    [Fact]
    public async Task GetQuantityAsync_ReturnsExistingQuantity_WhenItemExists()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 5);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var quantity = await CreateService().GetQuantityAsync(UserId, ItemKeys.ReforgeScroll);

        quantity.Should().Be(5);
    }

    [Fact]
    public async Task IncrementAsync_CreatesItem_WhenUserNeverOwnedIt()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateService().IncrementAsync(UserId, ItemKeys.DungeonStone, 3);

        result.Quantity.Should().Be(3);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncrementAsync_AddsToExistingQuantity()
    {
        var existing = InventoryItem.Create(UserId, ItemKeys.DungeonStone, quantity: 2);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateService().IncrementAsync(UserId, ItemKeys.DungeonStone, 4);

        result.Quantity.Should().Be(6);
        _inventoryRepository.Verify(r => r.Update(existing), Times.Once);
    }

    // RN-005: quantidade nunca pode ficar negativa.
    [Fact]
    public async Task IncrementAsync_Throws_WhenResultingQuantityWouldBeNegative()
    {
        var existing = InventoryItem.Create(UserId, ItemKeys.DungeonStone, quantity: 1);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => CreateService().IncrementAsync(UserId, ItemKeys.DungeonStone, -2);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // CA-002: um novo ItemKey deve ser representavel no inventario sem
    // nenhuma migracao de schema - basta declarar a constante e (opcionalmente)
    // registrar o metadado no ItemCatalog. O proprio InventoryItem/InventoryService
    // ja suportam qualquer string de ItemKey sem alteracao estrutural.
    [Fact]
    public async Task IncrementAsync_SupportsNewItemKey_WithoutSchemaChange()
    {
        const string newItemKey = "nova_chave_de_item_futura";
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, newItemKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateService().IncrementAsync(UserId, newItemKey, 1);

        result.ItemKey.Should().Be(newItemKey);
        result.Quantity.Should().Be(1);
    }

    // ─── US-227 / RN-006: retry de concorrencia otimista no inventario ────────

    [Fact]
    public async Task IncrementAsync_RetriesAndSucceeds_WhenConcurrencyConflictOccursOnce()
    {
        var existing = InventoryItem.Create(UserId, ItemKeys.DungeonStone, quantity: 2);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Simula o reload do EF Core restaurando a quantidade persistida (2)
        // antes da segunda tentativa, evitando incremento duplicado em memoria.
        _inventoryRepository
            .Setup(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => SetQuantity(existing, 2))
            .Returns(Task.CompletedTask);

        var saveAttempt = 0;
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                saveAttempt++;
                if (saveAttempt == 1)
                    throw new DbUpdateConcurrencyException("conflito simulado");

                return Task.FromResult(1);
            });

        var result = await CreateService().IncrementAsync(UserId, ItemKeys.DungeonStone, 3);

        result.Quantity.Should().Be(5);
        saveAttempt.Should().Be(2);
        _inventoryRepository.Verify(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncrementAsync_ThrowsConcurrencyConflictException_WhenRetriesExhausted()
    {
        var existing = InventoryItem.Create(UserId, ItemKeys.DungeonStone, quantity: 2);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _inventoryRepository
            .Setup(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => SetQuantity(existing, 2))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("conflito simulado"));

        var act = () => CreateService().IncrementAsync(UserId, ItemKeys.DungeonStone, 3);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        _inventoryRepository.Verify(
            r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    /// Simula a restauracao de estado feita por context.Entry(item).ReloadAsync()
    /// no EF Core real, onde as propriedades escalares (Quantity) sao
    /// sobrescritas com o valor persistido no banco.
    private static void SetQuantity(InventoryItem item, int quantity) =>
        typeof(InventoryItem).GetProperty(nameof(InventoryItem.Quantity))!
            .SetValue(item, quantity);
}
