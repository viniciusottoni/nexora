using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Commands.ProcessIapPurchase;
using Awaken.Application.Shop.Commands.PurchaseWithGold;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// US-190 — Testes de auditoria de eventos de economia.
///
/// CA-001: compra concedida gera 2 registros com o mesmo CorrelationId
///         (ShopOrder.Initiated + ShopOrder.Granted).
/// CA-002: metadados de auditoria de compra IAP não contêm token nem dado de pagamento.
/// CA-003: débito/crédito de Gold gera AuditLog referenciando a carteira e o motivo.
/// </summary>
public class EconomyAuditTests
{
    // ── Fixtures compartilhadas ──────────────────────────────────────────────
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
    private const string ProductKey = "reforja_scroll";
    private const string CorrelationId = "corr-audit-test-001";

    private static Mock<IUnitOfWorkTransaction> SetupTransaction(Mock<IUnitOfWork> unitOfWork)
    {
        var transaction = new Mock<IUnitOfWorkTransaction>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return transaction;
    }

    // ════════════════════════════════════════════════════════════════════════
    // CA-001 — PurchaseWithGoldCommandHandler: 2 registros com mesmo CorrelationId
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PurchaseWithGold_WhenGranted_RecordsTwoAuditEntriesWithSameCorrelationId()
    {
        // Arrange
        var auditLogService = new Mock<IAuditLogService>();
        var shopProductRepo = new Mock<IShopProductRepository>();
        var shopOrderRepo = new Mock<IShopOrderRepository>();
        var goldWalletService = new Mock<IGoldWalletService>();
        var inventoryService = new Mock<IInventoryService>();
        var revenueCatValidationService = new Mock<IRevenueCatValidationService>();
        var currentUserService = new Mock<ICurrentUserService>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        SetupTransaction(unitOfWork);

        currentUserService.Setup(s => s.UserId).Returns(UserId);
        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));
        revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(true, null, null, null, false));

        var product = ShopProduct.Create(ProductKey, "Pergaminho da Reforja", null,
            "consumable", "rare", null, UtcNow, 150);
        shopProductRepo.Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(200, "setup", null, null, null, UtcNow);
        var debitEntry = wallet.Debit(150, "shop_purchase", null, null, CorrelationId, UtcNow);
        goldWalletService
            .Setup(s => s.DebitAsync(UserId, 150, "shop_purchase",
                "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitEntry);

        var inventoryItem = InventoryItem.Create(UserId, ProductKey, 1);
        inventoryService
            .Setup(s => s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        // Capturar todas as chamadas de auditoria.
        var auditCalls = new List<(string Action, string ResourceType, Guid? ResourceId)>();
        auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (action, _, _, resourceType, resourceId, _, _) =>
                    auditCalls.Add((action, resourceType, resourceId)))
            .Returns(Task.CompletedTask);

        var handler = new PurchaseWithGoldCommandHandler(
            shopProductRepo.Object,
            shopOrderRepo.Object,
            goldWalletService.Object,
            inventoryService.Object,
            currentUserService.Object,
            dateTimeService.Object,
            unitOfWork.Object,
            httpContextAccessor.Object,
            auditLogService.Object,
            NullLogger<PurchaseWithGoldCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        // Assert — CA-001: exatamente 2 registros de auditoria (Initiated + Granted).
        auditCalls.Should().HaveCount(2);
        auditCalls.Should().ContainSingle(c => c.Action == AuditActions.ShopOrderInitiated);
        auditCalls.Should().ContainSingle(c => c.Action == AuditActions.ShopOrderGranted);

        // Ambos referem o mesmo ShopOrder (mesmo ResourceId).
        var orderId = result.OrderId;
        auditCalls.All(c => c.ResourceId == orderId).Should().BeTrue(
            "ambos os registros de auditoria devem referenciar o mesmo ShopOrder (CA-001)");
    }

    [Fact]
    public async Task PurchaseWithGold_WhenGranted_AuditActorTypeIsUser()
    {
        // Arrange
        var auditLogService = new Mock<IAuditLogService>();
        var shopProductRepo = new Mock<IShopProductRepository>();
        var shopOrderRepo = new Mock<IShopOrderRepository>();
        var goldWalletService = new Mock<IGoldWalletService>();
        var inventoryService = new Mock<IInventoryService>();
        var currentUserService = new Mock<ICurrentUserService>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        SetupTransaction(unitOfWork);

        currentUserService.Setup(s => s.UserId).Returns(UserId);
        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));

        var product = ShopProduct.Create(ProductKey, "Scroll", null, "consumable", "rare", null, UtcNow, 100);
        shopProductRepo.Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(200, "setup", null, null, null, UtcNow);
        var debitEntry = wallet.Debit(100, "shop_purchase", null, null, CorrelationId, UtcNow);
        goldWalletService
            .Setup(s => s.DebitAsync(UserId, 100, "shop_purchase", "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitEntry);

        var inventoryItem = InventoryItem.Create(UserId, ProductKey, 1);
        inventoryService.Setup(s => s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>())).ReturnsAsync(inventoryItem);

        var capturedActorTypes = new List<AuditActorType>();
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, actorType, _, _, _, _) => capturedActorTypes.Add(actorType))
            .Returns(Task.CompletedTask);

        var handler = new PurchaseWithGoldCommandHandler(
            shopProductRepo.Object, shopOrderRepo.Object, goldWalletService.Object,
            inventoryService.Object, currentUserService.Object, dateTimeService.Object,
            unitOfWork.Object, httpContextAccessor.Object, auditLogService.Object,
            NullLogger<PurchaseWithGoldCommandHandler>.Instance);

        // Act
        await handler.Handle(new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        // Assert — compra Gold usa ActorType = User.
        capturedActorTypes.Should().AllBeEquivalentTo(AuditActorType.User);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CA-002 — Metadados sanitizados (sem token/pagamento)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessIapPurchase_WhenGranted_AuditMetadataDoesNotContainTokenOrPaymentData()
    {
        // Arrange
        const string TransactionId = "TXN-SENSITIVE-999";
        const string IapProductKey = "premium_scroll_iap";

        var auditLogService = new Mock<IAuditLogService>();
        var ledgerRepo = new Mock<IIapTransactionLedgerRepository>();
        var shopProductRepo = new Mock<IShopProductRepository>();
        var inventoryRepo = new Mock<IInventoryRepository>();
        var shopOrderRepo = new Mock<IShopOrderRepository>();
        var revenueCatValidationService = new Mock<IRevenueCatValidationService>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));
        ledgerRepo.Setup(r => r.AddAsync(It.IsAny<IapTransactionLedger>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        ledgerRepo.Setup(r => r.Update(It.IsAny<IapTransactionLedger>()));
        inventoryRepo.Setup(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inventoryRepo.Setup(r => r.Update(It.IsAny<InventoryItem>()));
        revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(true, null, null, null, false));

        ledgerRepo.Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        shopOrderRepo.Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);

        var product = ShopProduct.Create(IapProductKey, "Scroll IAP", null, "consumable", "rare", "rc_premium_scroll", UtcNow);
        shopProductRepo.Setup(r => r.GetByKeyAsync(IapProductKey, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        inventoryRepo.Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, IapProductKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Capturar todos os metadados de auditoria.
        var capturedMetadata = new List<string?>();
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, _, _, _, metadata, _) => capturedMetadata.Add(metadata))
            .Returns(Task.CompletedTask);

        var sender = new Mock<MediatR.ISender>();

        var handler = new ProcessIapPurchaseCommandHandler(
            ledgerRepo.Object, shopProductRepo.Object, inventoryRepo.Object,
            shopOrderRepo.Object, revenueCatValidationService.Object, sender.Object,
            dateTimeService.Object, unitOfWork.Object,
            httpContextAccessor.Object, auditLogService.Object,
            NullLogger<ProcessIapPurchaseCommandHandler>.Instance);

        // Act
        await handler.Handle(
            new ProcessIapPurchaseCommand(UserId, TransactionId, IapProductKey, "google_play"),
            CancellationToken.None);

        // Assert — CA-002: nenhum metadado contém o TransactionId (token de pagamento) nem "token".
        capturedMetadata.Should().NotBeEmpty();
        capturedMetadata.Should().AllSatisfy(m =>
        {
            m.Should().NotContain(TransactionId,
                "TransactionId é dado de pagamento e não deve aparecer nos metadados (ADR-015)");
            m.Should().NotContainAny(new[] { "token", "Token", "TOKEN" },
                "metadados não devem conter tokens (ADR-015 / CA-002)");
            m.Should().NotContainAny(new[] { "receipt", "Receipt", "payment", "Payment" },
                "metadados não devem conter dados de pagamento (ADR-015 / CA-002)");
        });

        // Metadados devem conter apenas productKey e channel.
        capturedMetadata.Should().AllSatisfy(m =>
        {
            m.Should().Contain(IapProductKey);
            m.Should().Contain("iap");
        });
    }

    [Fact]
    public async Task ProcessIapPurchase_WhenGranted_UsesSystemActorType()
    {
        // Arrange — concessões IAP via webhook devem usar ActorType = System (RN-003).
        const string TransactionId = "TXN-SYSTEM-001";
        const string IapProductKey = "slot_iap";

        var auditLogService = new Mock<IAuditLogService>();
        var ledgerRepo = new Mock<IIapTransactionLedgerRepository>();
        var shopProductRepo = new Mock<IShopProductRepository>();
        var inventoryRepo = new Mock<IInventoryRepository>();
        var shopOrderRepo = new Mock<IShopOrderRepository>();
        var revenueCatValidationService = new Mock<IRevenueCatValidationService>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));
        ledgerRepo.Setup(r => r.AddAsync(It.IsAny<IapTransactionLedger>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        ledgerRepo.Setup(r => r.Update(It.IsAny<IapTransactionLedger>()));
        inventoryRepo.Setup(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        inventoryRepo.Setup(r => r.Update(It.IsAny<InventoryItem>()));
        revenueCatValidationService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatTransactionValidation(true, null, null, null, false));

        ledgerRepo.Setup(r => r.GetByTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapTransactionLedger?)null);
        shopOrderRepo.Setup(r => r.GetByExternalTransactionIdAsync(TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopOrder?)null);

        // Produto tipo "slot" — sem inventário individual.
        var product = ShopProduct.Create(IapProductKey, "Slot IAP", null, "slot", "common", "rc_slot", UtcNow);
        shopProductRepo.Setup(r => r.GetByKeyAsync(IapProductKey, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var capturedActorTypes = new List<AuditActorType>();
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, actorType, _, _, _, _) => capturedActorTypes.Add(actorType))
            .Returns(Task.CompletedTask);

        var sender = new Mock<MediatR.ISender>();

        var handler = new ProcessIapPurchaseCommandHandler(
            ledgerRepo.Object, shopProductRepo.Object, inventoryRepo.Object,
            shopOrderRepo.Object, revenueCatValidationService.Object, sender.Object,
            dateTimeService.Object, unitOfWork.Object,
            httpContextAccessor.Object, auditLogService.Object,
            NullLogger<ProcessIapPurchaseCommandHandler>.Instance);

        // Act
        await handler.Handle(
            new ProcessIapPurchaseCommand(UserId, TransactionId, IapProductKey, "app_store"),
            CancellationToken.None);

        // Assert — RN-003: concessões automáticas usam ActorType = System.
        capturedActorTypes.Should().NotBeEmpty();
        capturedActorTypes.Should().AllBeEquivalentTo(AuditActorType.System);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CA-003 — GoldWalletService: débito auditado referenciando a carteira
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GoldWalletService_WhenDebited_RecordsAuditWithWalletIdAndReason()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        // Adicionar saldo suficiente para débito.
        wallet.Credit(500, "setup", null, null, null, UtcNow);

        var walletRepository = new Mock<IGoldWalletRepository>();
        var ledgerRepository = new Mock<IGoldLedgerEntryRepository>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var auditLogService = new Mock<IAuditLogService>();

        walletRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        walletRepository.Setup(r => r.Update(It.IsAny<GoldWallet>()));
        ledgerRepository.Setup(r => r.AddAsync(It.IsAny<GoldLedgerEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        // Capturar chamada de auditoria.
        string? capturedAction = null;
        string? capturedResourceType = null;
        Guid? capturedResourceId = null;
        string? capturedMetadata = null;
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (action, _, _, resourceType, resourceId, metadata, _) =>
                {
                    capturedAction = action;
                    capturedResourceType = resourceType;
                    capturedResourceId = resourceId;
                    capturedMetadata = metadata;
                })
            .Returns(Task.CompletedTask);

        var service = new GoldWalletService(
            walletRepository.Object,
            ledgerRepository.Object,
            dateTimeService.Object,
            unitOfWork.Object,
            httpContextAccessor.Object,
            auditLogService.Object,
            NullLogger<GoldWalletService>.Instance);

        // Act
        await service.DebitAsync(UserId, 100, "shop_purchase", "shop_order", null,
            CancellationToken.None);

        // Assert — CA-003: auditoria de débito referenciando a carteira e o motivo.
        capturedAction.Should().Be(AuditActions.GoldWalletDebited);
        capturedResourceType.Should().Be(AuditResourceTypes.GoldWallet);
        capturedResourceId.Should().Be(wallet.Id);

        // Metadados sem valor de saldo (ADR-015).
        capturedMetadata.Should().Contain("shop_purchase", "motivo deve constar nos metadados");
        capturedMetadata.Should().Contain("shop_order", "referenceType deve constar nos metadados");
        capturedMetadata.Should().NotContain("100", "valor de Gold não deve aparecer nos metadados (ADR-015)");
        capturedMetadata.Should().NotContain("balance", "saldo não deve aparecer nos metadados (ADR-015)");
    }

    [Fact]
    public async Task GoldWalletService_WhenCredited_RecordsAuditWithWalletIdAndReason()
    {
        // Arrange
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);

        var walletRepository = new Mock<IGoldWalletRepository>();
        var ledgerRepository = new Mock<IGoldLedgerEntryRepository>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var auditLogService = new Mock<IAuditLogService>();

        walletRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        walletRepository.Setup(r => r.Update(It.IsAny<GoldWallet>()));
        ledgerRepository.Setup(r => r.AddAsync(It.IsAny<GoldLedgerEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        string? capturedAction = null;
        string? capturedResourceType = null;
        Guid? capturedResourceId = null;
        string? capturedMetadata = null;
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (action, _, _, resourceType, resourceId, metadata, _) =>
                {
                    capturedAction = action;
                    capturedResourceType = resourceType;
                    capturedResourceId = resourceId;
                    capturedMetadata = metadata;
                })
            .Returns(Task.CompletedTask);

        var service = new GoldWalletService(
            walletRepository.Object,
            ledgerRepository.Object,
            dateTimeService.Object,
            unitOfWork.Object,
            httpContextAccessor.Object,
            auditLogService.Object,
            NullLogger<GoldWalletService>.Instance);

        // Act
        await service.CreditAsync(UserId, 250, "quest_reward", "quest", null,
            CancellationToken.None);

        // Assert — CA-003: auditoria de crédito referenciando a carteira e o motivo.
        capturedAction.Should().Be(AuditActions.GoldWalletCredited);
        capturedResourceType.Should().Be(AuditResourceTypes.GoldWallet);
        capturedResourceId.Should().Be(wallet.Id);

        capturedMetadata.Should().Contain("quest_reward", "motivo deve constar nos metadados");
        capturedMetadata.Should().Contain("quest", "referenceType deve constar nos metadados");
        // Sem valor de saldo/quantidade.
        capturedMetadata.Should().NotContain("250", "valor de Gold não deve aparecer nos metadados (ADR-015)");
    }

    // ════════════════════════════════════════════════════════════════════════
    // RN-005 — falha ao auditar é observável, mas não cancela a transação
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PurchaseWithGold_WhenAuditFails_DoesNotCancelMainTransaction()
    {
        // Arrange — auditLogService lança exceção em todas as chamadas.
        var auditLogService = new Mock<IAuditLogService>();
        var shopProductRepo = new Mock<IShopProductRepository>();
        var shopOrderRepo = new Mock<IShopOrderRepository>();
        var goldWalletService = new Mock<IGoldWalletService>();
        var inventoryService = new Mock<IInventoryService>();
        var currentUserService = new Mock<ICurrentUserService>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        SetupTransaction(unitOfWork);

        currentUserService.Setup(s => s.UserId).Returns(UserId);
        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));

        var product = ShopProduct.Create(ProductKey, "Scroll", null, "consumable", "rare", null, UtcNow, 50);
        shopProductRepo.Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(200, "setup", null, null, null, UtcNow);
        var debitEntry = wallet.Debit(50, "shop_purchase", null, null, CorrelationId, UtcNow);
        goldWalletService
            .Setup(s => s.DebitAsync(UserId, 50, "shop_purchase", "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitEntry);

        var inventoryItem = InventoryItem.Create(UserId, ProductKey, 1);
        inventoryService
            .Setup(s => s.IncrementAsync(UserId, ProductKey, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        // Auditoria lança exceção.
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Audit storage indisponível"));

        var handler = new PurchaseWithGoldCommandHandler(
            shopProductRepo.Object, shopOrderRepo.Object, goldWalletService.Object,
            inventoryService.Object, currentUserService.Object, dateTimeService.Object,
            unitOfWork.Object, httpContextAccessor.Object, auditLogService.Object,
            NullLogger<PurchaseWithGoldCommandHandler>.Instance);

        // Act — a compra deve completar mesmo com falha de auditoria (RN-005).
        var act = () => handler.Handle(new PurchaseWithGoldCommand(ProductKey), CancellationToken.None);

        // Assert
        var result = await act.Should().NotThrowAsync();
        result.Which.Status.Should().Be("granted",
            "a transação principal deve completar mesmo quando a auditoria falha (RN-005)");
    }

    [Fact]
    public async Task PurchaseWithGold_WhenInsufficientBalance_RecordsFailedAuditEntry()
    {
        // Arrange
        var auditLogService = new Mock<IAuditLogService>();
        var shopProductRepo = new Mock<IShopProductRepository>();
        var shopOrderRepo = new Mock<IShopOrderRepository>();
        var goldWalletService = new Mock<IGoldWalletService>();
        var inventoryService = new Mock<IInventoryService>();
        var currentUserService = new Mock<ICurrentUserService>();
        var dateTimeService = new Mock<IDateTimeService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        SetupTransaction(unitOfWork);

        currentUserService.Setup(s => s.UserId).Returns(UserId);
        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = CorrelationId;
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        shopOrderRepo.Setup(r => r.AddAsync(It.IsAny<ShopOrder>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        shopOrderRepo.Setup(r => r.Update(It.IsAny<ShopOrder>()));

        var product = ShopProduct.Create(ProductKey, "Scroll", null, "consumable", "rare", null, UtcNow, 500);
        shopProductRepo.Setup(r => r.GetByKeyAsync(ProductKey, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        goldWalletService
            .Setup(s => s.DebitAsync(UserId, 500, "shop_purchase", "shop_order", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientGoldBalanceException(UserId, currentBalance: 0, requestedAmount: 500));

        var capturedActions = new List<string>();
        auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (action, _, _, _, _, _, _) => capturedActions.Add(action))
            .Returns(Task.CompletedTask);

        var handler = new PurchaseWithGoldCommandHandler(
            shopProductRepo.Object, shopOrderRepo.Object, goldWalletService.Object,
            inventoryService.Object, currentUserService.Object, dateTimeService.Object,
            unitOfWork.Object, httpContextAccessor.Object, auditLogService.Object,
            NullLogger<PurchaseWithGoldCommandHandler>.Instance);

        // Act
        await Assert.ThrowsAsync<InsufficientGoldBalanceException>(() =>
            handler.Handle(new PurchaseWithGoldCommand(ProductKey), CancellationToken.None));

        // Assert — deve ter gerado: Initiated + Failed (sem Granted).
        capturedActions.Should().Contain(AuditActions.ShopOrderInitiated);
        capturedActions.Should().Contain(AuditActions.ShopOrderFailed);
        capturedActions.Should().NotContain(AuditActions.ShopOrderGranted);
    }
}
