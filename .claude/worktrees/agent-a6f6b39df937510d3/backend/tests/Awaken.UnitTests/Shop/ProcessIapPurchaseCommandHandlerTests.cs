using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Commands.CreditGoldFromPurchase;
using Awaken.Application.Shop.Commands.ProcessIapPurchase;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// Testes unitários para ProcessIapPurchaseCommandHandler.
/// CA-001: ShopOrder é criado com canal "iap" e correlação.
/// CA-002: mesma transação IAP não concede o benefício duas vezes.
/// </summary>
public class ProcessIapPurchaseCommandHandlerTests
{
    private readonly Mock<IIapTransactionLedgerRepository> _ledgerRepo = new();
    private readonly Mock<IShopProductRepository> _shopProductRepo = new();
    private readonly Mock<IInventoryRepository> _inventoryRepo = new();
    private readonly Mock<IShopOrderRepository> _shopOrderRepo = new();
    private readonly Mock<IRevenueCatValidationService> _revenueCatValidationService = new();
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
    private const string TransactionId = "TXN-TEST-001";
    private const string ProductKey = "reforja_scroll_iap";
    private const string Store = "google_play";
    private const string CorrelationId = "corr-iap-test";

    public ProcessIapPurchaseCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));
        _ledgerRepo.Setup(r => r.AddAsync(It.IsAny<IapTransactionLedger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ledgerRepo.Setup(r => r.Update(It.IsAny<IapTransactionLedger>()));
        _inventoryRepo.Setup(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _inventoryRepo.Setup(r => r.Update(It.IsAny<InventoryItem>()));
        _revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(true, null, null, null, false));

        // Default: auditLogService.RecordAsync sempre completa.
        _auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ProcessIapPurchaseCommandHandler CreateHandler() =>
        new(
            _ledgerRepo.Object,
            _shopProductRepo.Object,
            _inventoryRepo.Object,
            _shopOrderRepo.Object,
            _revenueCatValidationService.Object,
            _sender.Object,
            _dateTimeService.Object,
            _unitOfWork.Object,
            _httpContextAccessor.Object,
            _auditLogService.Object,
            NullLogger<ProcessIapPurchaseCommandHandler>.Instance);

    private ProcessIapPurchaseCommand CreateCommand() =>
        new(UserId, TransactionId, ProductKey, Store);

    private static ShopProduct CreateIapProduct() =>
        ShopProduct.Create(ProductKey, "Pergaminho IAP", null,
            "consumable", "rare", "rc_reforja", UtcNow);

    private static ShopProduct CreateGoldPackProduct(int goldAmount = 500) =>
        ShopProduct.Create(ProductKey, "Pacote de Gold", null,
            "consumable", "rare", "rc_gold_pack_500", UtcNow, goldAmount: goldAmount);

    private void SetupFirstPurchase(ShopProduct product)
    {
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    // ─── CA-001: ShopOrder criado com canal "iap" ─────────────────────────────

    [Fact]
    public async Task Handle_WhenFirstPurchase_CreatesShopOrderWithIapChannel()
    {
        // Arrange
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);

        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateIapProduct());
        _inventoryRepo
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        ShopOrder? capturedOrder = null;
        _shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ShopOrder, CancellationToken>((o, _) => capturedOrder = o)
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — CA-001
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Channel.Should().Be("iap");
        capturedOrder.ProductKey.Should().Be(ProductKey);
        capturedOrder.ExternalTransactionId.Should().Be(TransactionId);
        capturedOrder.CorrelationId.Should().Be(CorrelationId);
        capturedOrder.UserId.Should().Be(UserId);
    }

    // ─── CA-002: idempotência IAP ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenShopOrderAlreadyGranted_ReturnsEarlyWithoutGrantingAgain()
    {
        // Arrange — ShopOrder já existe em "granted".
        var existingOrder = ShopOrder.Create(UserId, "iap", ProductKey, TransactionId, CorrelationId, UtcNow);
        existingOrder.MarkGranted(UtcNow);

        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);

        // Act
        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — CA-002: nenhum novo ShopOrder criado, nenhum item concedido.
        _shopOrderRepo.Verify(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepo.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepo.Verify(r => r.Update(It.IsAny<InventoryItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIapLedgerAlreadyGrantedButNoShopOrder_ReturnsEarlyWithoutGrantingAgain()
    {
        // Arrange — ledger legado "granted", sem ShopOrder correspondente.
        var existingLedger = IapTransactionLedger.Create(UserId, TransactionId, ProductKey, Store, UtcNow);
        existingLedger.MarkGranted(UtcNow);

        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLedger);

        // Act
        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — CA-002: nenhum item concedido via inventário.
        _inventoryRepo.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepo.Verify(r => r.Update(It.IsAny<InventoryItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFirstPurchase_GrantsItemViaInventory()
    {
        // Arrange
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateIapProduct());
        _inventoryRepo
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — item adicionado ao inventário.
        _inventoryRepo.Verify(r => r.AddAsync(
            It.Is<InventoryItem>(i => i.ItemKey == ProductKey && i.UserId == UserId && i.Quantity == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── US-226: compra aprovada credita Gold (RN-001/RN-002/RN-007) ──────────

    [Fact]
    public async Task Handle_WhenProductIsGoldPack_CreditsGoldViaDedicatedCommandWithAmountFromCatalog()
    {
        // Arrange — produto é um pacote de Gold (GoldAmount=500 no catálogo).
        var product = CreateGoldPackProduct(goldAmount: 500);
        SetupFirstPurchase(product);

        // Act
        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — RN-001/RN-002: quantidade creditada vem exclusivamente do
        // catálogo (ShopProduct.GoldAmount), nunca do payload do app — o comando
        // CreditGoldFromPurchaseCommand não tem nenhum campo preenchido a partir
        // de ProcessIapPurchaseCommand (que não carrega quantidade nenhuma).
        _sender.Verify(s => s.Send(
            It.Is<CreditGoldFromPurchaseCommand>(c => c.UserId == UserId && c.Amount == 500),
            It.IsAny<CancellationToken>()), Times.Once);

        // Não deve conceder via inventário quando é pacote de Gold.
        _inventoryRepo.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepo.Verify(r => r.Update(It.IsAny<InventoryItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductIsGoldPack_MarksOrderGrantedAndReturnsGrantedStatus()
    {
        // Arrange
        var product = CreateGoldPackProduct(goldAmount: 250);
        SetupFirstPurchase(product);

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.Status.Should().Be("granted");
        result.Channel.Should().Be("iap");
        result.ProductKey.Should().Be(ProductKey);
        _shopOrderRepo.Verify(r => r.Update(
            It.Is<ShopOrder>(o => o.Status == "granted")), Times.AtLeastOnce);
    }

    // ─── Compra pendente (provider indisponível) ───────────────────────────────

    [Fact]
    public async Task Handle_WhenProviderThrows_ReturnsPendingStatusWithoutCreditingGold()
    {
        // Arrange — RevenueCat indisponível (exceção) → falha temporária do provider.
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);
        _revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("RevenueCat unavailable"));

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — pedido permanece "pending" (erro recuperável, não credita).
        result.Status.Should().Be("pending");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryRepo.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Compra negada (validação falhou) ──────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsFailedStatusWithoutCreditingGold()
    {
        // Arrange
        var product = CreateGoldPackProduct();
        SetupFirstPurchase(product);
        _revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(false, null, null, null, false));

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.Status.Should().Be("failed");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── RN-005: transação validada para outro usuário não credita ────────────

    [Fact]
    public async Task Handle_WhenValidatedAppUserIdDoesNotMatchRequestUser_RejectsAsUserMismatch()
    {
        // Arrange — RevenueCat valida a transação, mas para outro AppUserId.
        var product = CreateGoldPackProduct();
        SetupFirstPurchase(product);

        var otherUserId = Guid.NewGuid();
        _revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(true, "rc_gold_pack_500", otherUserId.ToString(), "google_play", false));

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — RN-005: bloqueado, nenhum crédito.
        result.Status.Should().Be("failed");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidatedAppUserIdMatchesRequestUser_GrantsNormally()
    {
        // Arrange — AppUserId corresponde ao usuário da requisição.
        var product = CreateGoldPackProduct();
        SetupFirstPurchase(product);
        _revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(true, "rc_gold_pack_500", UserId.ToString(), "google_play", false));

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.Status.Should().Be("granted");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Transação repetida (idempotência) ─────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTransactionAlreadyGranted_DoesNotCreditGoldAgain()
    {
        // Arrange
        var existingOrder = ShopOrder.Create(UserId, "iap", ProductKey, TransactionId, CorrelationId, UtcNow);
        existingOrder.MarkGranted(UtcNow);
        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — CA-002/RN-004: nenhum novo crédito de Gold.
        result.Status.Should().Be("granted");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Produto inativo (RN-006) ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProductIsInactive_BlocksCreditAndMarksOrderFailed()
    {
        // Arrange
        var product = CreateGoldPackProduct();
        product.Deactivate(UtcNow);
        SetupFirstPurchase(product);

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert — RN-006: produto inativo bloqueia crédito.
        result.Status.Should().Be("failed");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Produto divergente (GoldAmount inválido) — RN-006 ─────────────────────

    [Fact]
    public async Task Handle_WhenGoldAmountIsZeroOrNegative_BlocksCreditAsProductMismatch()
    {
        // Arrange — produto mal configurado (GoldAmount <= 0).
        var product = CreateGoldPackProduct(goldAmount: 0);
        SetupFirstPurchase(product);

        // Act
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.Status.Should().Be("failed");
        _sender.Verify(s => s.Send(It.IsAny<CreditGoldFromPurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Quantidade adulterada: nada no payload do app controla o valor ───────

    [Fact]
    public async Task Handle_AppCannotInfluenceCreditedAmount_OnlyCatalogGoldAmountIsUsed()
    {
        // Arrange — ProcessIapPurchaseCommand não tem (e nunca teve) nenhum campo
        // de quantidade; mesmo que o "Store"/"TransactionId" sejam manipulados
        // pelo app, o valor creditado só pode vir de ShopProduct.GoldAmount.
        var product = CreateGoldPackProduct(goldAmount: 777);
        SetupFirstPurchase(product);

        var tamperedCommand = new ProcessIapPurchaseCommand(
            UserId, TransactionId, ProductKey, "store_with_injected_amount=999999");

        // Act
        await CreateHandler().Handle(tamperedCommand, CancellationToken.None);

        // Assert — quantidade creditada é exatamente a do catálogo (777), nunca
        // qualquer valor que pudesse ser inferido do campo "Store" ou de outro
        // campo do payload do app.
        _sender.Verify(s => s.Send(
            It.Is<CreditGoldFromPurchaseCommand>(c => c.Amount == 777),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Produto não encontrado ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProductNotFound_ThrowsNotFoundExceptionAndMarksOrderFailed()
    {
        // Arrange
        _ledgerRepo
            .Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        _shopOrderRepo
            .Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);
        _shopProductRepo
            .Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopProduct?)null);

        // Act
        var act = async () => await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _shopOrderRepo.Verify(r => r.Update(It.Is<ShopOrder>(o => o.Status == "failed")), Times.Once);
    }
}
