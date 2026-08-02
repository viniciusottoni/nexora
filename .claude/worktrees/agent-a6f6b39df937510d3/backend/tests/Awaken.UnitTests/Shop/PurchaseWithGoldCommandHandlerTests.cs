using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Commands.PurchaseWithGold;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// Testes unitários para PurchaseWithGoldCommandHandler.
/// CA-001: pedido rastreável com status e correlação + 2 registros de auditoria (US-190).
/// CA-002: metadados de auditoria sem token ou dado de pagamento (US-190).
/// CA-003: saldo insuficiente → pedido failed, sem débito nem concessão.
/// </summary>
public class PurchaseWithGoldCommandHandlerTests
{
    private readonly Mock<IShopProductRepository> _shopProductRepo = new();
    private readonly Mock<IShopOrderRepository> _shopOrderRepo = new();
    private readonly Mock<IGoldWalletService> _goldWalletService = new();
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
    private const string ProductKey = "reforja_scroll";
    private const string CorrelationId = "corr-test-001";

    public PurchaseWithGoldCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        // Simular HttpContext com CorrelationId.
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);
        _transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        // Default: shopOrderRepo.AddAsync não faz nada.
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));

        // Default: auditLogService.RecordAsync sempre completa.
        _auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private PurchaseWithGoldCommandHandler CreateHandler() =>
        new(
            _shopProductRepo.Object,
            _shopOrderRepo.Object,
            _goldWalletService.Object,
            _inventoryService.Object,
            _currentUserService.Object,
            _dateTimeService.Object,
            _unitOfWork.Object,
            _httpContextAccessor.Object,
            _auditLogService.Object,
            NullLogger<PurchaseWithGoldCommandHandler>.Instance);

    private static ShopProduct CreateGoldProduct(int priceGold = 150) =>
        ShopProduct.Create(ProductKey, "Pergaminho da Reforja", null,
            "consumable", "rare", null, UtcNow, priceGold);

    private static InventoryItem CreateInventoryItem(Guid userId, string key, int qty = 0) =>
        InventoryItem.Create(userId, key, qty);

    // ─── CA-001: pedido rastreável ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProductActiveAndSufficientBalance_CreatesGrantedShopOrder()
    {
        // Arrange
        var product = CreateGoldProduct(150);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var inventoryItem = CreateInventoryItem(UserId, ProductKey, 2);
        _inventoryService
            .Setup(s => s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        // Simular débito com sucesso.
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        // Precisamos que Debit não lance — retornamos uma entrada de ledger fake.
        var ledgerEntry = wallet.Credit(200, "setup", null, null, null, UtcNow); // coloca saldo
        var debitEntry = wallet.Debit(150, "shop_purchase", null, null, CorrelationId, UtcNow);
        _goldWalletService
            .Setup(s => s.DebitAsync(UserId, 150, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitEntry);

        ShopOrder? capturedOrder = null;
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ShopOrder, CancellationToken>((o, _) => capturedOrder = o)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        // Assert — CA-001: pedido rastreável com canal, produto, status, correlação.
        result.Should().NotBeNull();
        result.Channel.Should().Be("gold");
        result.ProductKey.Should().Be(ProductKey);
        result.Status.Should().Be("granted");
        result.CorrelationId.Should().Be(CorrelationId);
        result.OrderId.Should().NotBeEmpty();

        capturedOrder.Should().NotBeNull();
        capturedOrder!.UserId.Should().Be(UserId);
        capturedOrder.Channel.Should().Be("gold");
        capturedOrder.CorrelationId.Should().Be(CorrelationId);

        _inventoryService.Verify(s =>
            s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>()), Times.Once);

        // US-227 / RN-001: a transação deve ter sido aberta e confirmada (commit).
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopProduct?)null);

        // Act
        var act = () => CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _shopOrderRepo.Verify(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()), Times.Never);
        _goldWalletService.Verify(s => s.DebitAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductIsGoldOnlyButInactive_ThrowsNotFoundException()
    {
        // Arrange — produto sem PriceGold (canal IAP, não Gold)
        var iapProduct = ShopProduct.Create("reforja_scroll_iap", "Pergaminho IAP", null,
            "consumable", "rare", "rc_product", UtcNow); // priceGold = null → canal IAP
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(iapProduct);

        // Act
        var act = () => CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── CA-003: Gold sem saldo ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInsufficientBalance_MarksOrderFailed_AndDoesNotGrantItem()
    {
        // Arrange
        var product = CreateGoldProduct(150);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // DebitAsync lança InsufficientGoldBalanceException.
        _goldWalletService
            .Setup(s => s.DebitAsync(UserId, 150, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientGoldBalanceException(UserId, currentBalance: 50, requestedAmount: 150));

        ShopOrder? capturedOrder = null;
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ShopOrder, CancellationToken>((o, _) => capturedOrder = o)
            .Returns(Task.CompletedTask);

        // Act — deve relançar InsufficientGoldBalanceException.
        var act = () => CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientGoldBalanceException>();

        // Assert — CA-003: pedido failed, sem concessão de item.
        _shopOrderRepo.Verify(r => r.Update(It.Is<ShopOrder>(o => o.Status == "failed")), Times.Once);
        _inventoryService.Verify(s =>
            s.IncrementAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_OrderIsCreatedBeforeDebit()
    {
        // Arrange — garante que o pedido já foi salvo antes do débito falhar.
        var product = CreateGoldProduct(500);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _goldWalletService
            .Setup(s => s.DebitAsync(UserId, 500, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientGoldBalanceException(UserId, currentBalance: 0, requestedAmount: 500));

        int addAsyncCallCount = 0;
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ShopOrder, CancellationToken>((_, _) => addAsyncCallCount++)
            .Returns(Task.CompletedTask);

        // Act
        await Assert.ThrowsAsync<InsufficientGoldBalanceException>(() =>
            CreateHandler().Handle(new PurchaseWithGoldCommand(ProductKey), CancellationToken.None));

        // Assert — ShopOrder foi adicionado antes do débito.
        addAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_RollsBackTransaction()
    {
        // US-227 / RN-002: mesmo a falha "esperada" de saldo insuficiente deve
        // reverter a transação aberta antes do débito (nenhuma sobra de
        // transação aberta sem commit/rollback).
        var product = CreateGoldProduct(150);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _goldWalletService
            .Setup(s => s.DebitAsync(UserId, 150, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientGoldBalanceException(UserId, currentBalance: 50, requestedAmount: 150));

        var act = () => CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientGoldBalanceException>();

        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── RN-001/RN-002: falha apos debito nao deixa Gold perdido sem item ──────

    [Fact]
    public async Task Handle_WhenInventoryGrantFailsAfterDebit_RollsBackTransaction_AndMarksOrderFailed()
    {
        // Arrange — débito bem-sucedido, mas concessão de item falha (ex.: erro
        // de infra no IInventoryService). RN-001/RN-002: o Gold não pode ficar
        // debitado permanentemente se o item não foi concedido.
        var product = CreateGoldProduct(150);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(200, "setup", null, null, null, UtcNow);
        var debitEntry = wallet.Debit(150, "shop_purchase", null, null, CorrelationId, UtcNow);
        _goldWalletService
            .Setup(s => s.DebitAsync(UserId, 150, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitEntry);

        _inventoryService
            .Setup(s => s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada de infra no inventario"));

        // Act
        var act = () => CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        // Assert — RN-001/RN-002: rollback da transação (débito revertido),
        // pedido marcado como failed, commit nunca chamado.
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _shopOrderRepo.Verify(r => r.Update(It.Is<ShopOrder>(o => o.Status == "failed")), Times.Once);
    }

    // ─── US-227 / RN-003: idempotência por chave de compra ────────────────────

    [Fact]
    public async Task Handle_WhenIdempotencyKeyMatchesExistingOrder_ReturnsExistingOrder_WithoutDebitingOrGranting()
    {
        const string idempotencyKey = "idem-key-001";

        var product = CreateGoldProduct(150);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var existingOrder = ShopOrder.Create(
            UserId, "gold", ProductKey, externalTransactionId: idempotencyKey,
            correlationId: CorrelationId, UtcNow);
        existingOrder.MarkGranted(UtcNow);

        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey, idempotencyKey), CancellationToken.None);

        // Assert — pedido existente retornado sem nova concessão (RN-003).
        result.OrderId.Should().Be(existingOrder.Id);
        result.Status.Should().Be("granted");

        _shopOrderRepo.Verify(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()), Times.Never);
        _goldWalletService.Verify(s => s.DebitAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryService.Verify(s =>
            s.IncrementAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyProvided_AndNoExistingOrder_CreatesOrderWithKeyAsExternalTransactionId()
    {
        const string idempotencyKey = "idem-key-002";

        var product = CreateGoldProduct(150);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);

        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(200, "setup", null, null, null, UtcNow);
        var debitEntry = wallet.Debit(150, "shop_purchase", null, null, CorrelationId, UtcNow);
        _goldWalletService
            .Setup(s => s.DebitAsync(UserId, 150, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitEntry);

        _inventoryService
            .Setup(s => s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInventoryItem(UserId, ProductKey, 1));

        ShopOrder? capturedOrder = null;
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ShopOrder, CancellationToken>((o, _) => capturedOrder = o)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(
            new PurchaseWithGoldCommand(ProductKey, idempotencyKey), CancellationToken.None);

        // Assert
        result.Status.Should().Be("granted");
        capturedOrder.Should().NotBeNull();
        capturedOrder!.ExternalTransactionId.Should().Be(idempotencyKey);
    }
}
