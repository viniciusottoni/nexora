using Awaken.Application.Admin.Economy.Commands.ReconcileGoldEconomy;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Economy;

/// <summary>
/// US-228: cobre os critérios de teste da seção 13 — saldo consistente, saldo divergente,
/// saldo negativo, pedido sem débito, crédito sem origem/validação, item sem origem,
/// volume anormal, muitas falhas, e não-duplicação de alertas já abertos recentes.
/// </summary>
public class ReconcileGoldEconomyCommandHandlerTests
{
    private readonly Mock<IGoldWalletRepository> _goldWalletRepository = new();
    private readonly Mock<IGoldLedgerEntryRepository> _goldLedgerEntryRepository = new();
    private readonly Mock<IShopOrderRepository> _shopOrderRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    public ReconcileGoldEconomyCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        // Por padrão, nenhuma entidade existe (cada teste popula o que precisa).
        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _inventoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Por padrão, nenhum alerta duplicado existe ainda.
        _securityAlertRepository
            .Setup(r => r.HasOpenRecentAlertAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private ReconcileGoldEconomyCommandHandler CreateHandler() => new(
        _goldWalletRepository.Object,
        _goldLedgerEntryRepository.Object,
        _shopOrderRepository.Object,
        _inventoryRepository.Object,
        _securityAlertRepository.Object,
        _auditLogService.Object,
        _unitOfWork.Object,
        _dateTimeService.Object);

    private static GoldWallet CreateWallet(Guid userId, long balance, DateTime utcNow)
    {
        var wallet = GoldWallet.CreateEmpty(userId, utcNow);
        if (balance > 0) wallet.Credit(balance, "seed", null, null, null, utcNow);
        return wallet;
    }

    // ── Saldo consistente: nenhum alerta ───────────────────────────────────────

    [Fact]
    public async Task Handle_BalanceMatchesLedger_CreatesNoAlert()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 100, UtcNow.AddDays(-1));

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([wallet]);

        var ledgerEntry = GetLedgerEntryFromWallet(wallet);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([ledgerEntry]);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreated.Should().Be(0);
        _securityAlertRepository.Verify(r => r.AddAsync(It.IsAny<SecurityAlert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── RN-001: saldo divergente ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_BalanceDivergesFromLastLedgerEntry_CreatesBalanceMismatchAlert()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 100, UtcNow.AddDays(-1));

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([wallet]);

        // Ledger registra BalanceAfter=50, mas wallet.Balance é 100 (divergência).
        var entry = CreateLedgerEntry(wallet.Id, GoldLedgerDirection.Credit, 50, 50, UtcNow.AddDays(-2));
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([entry]);

        SecurityAlert? captured = null;
        _securityAlertRepository.Setup(r => r.AddAsync(It.IsAny<SecurityAlert>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAlert, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreated.Should().Be(1);
        captured.Should().NotBeNull();
        captured!.AlertType.Should().Be(GoldEconomyAlertTypes.BalanceMismatch);
        captured.Severity.Should().Be("high");
        captured.AffectedUserId.Should().Be(userId);
    }

    // ── Saldo negativo (defensivo) ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WalletHasNegativeBalance_CreatesNegativeBalanceAlertWithCriticalSeverity()
    {
        var userId = Guid.NewGuid();
        // GoldWallet.Debit nunca permite saldo negativo via API pública — simulamos via reflection
        // o cenário defensivo de saldo negativo chegando por outro caminho (ex. migração de dados).
        var wallet = GoldWallet.CreateEmpty(userId, UtcNow.AddDays(-1));
        typeof(GoldWallet).GetProperty(nameof(GoldWallet.Balance))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(wallet, [(long)-10]);

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([wallet]);

        SecurityAlert? captured = null;
        _securityAlertRepository.Setup(r => r.AddAsync(It.IsAny<SecurityAlert>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAlert, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.NegativeBalance].Should().Be(1);
        captured.Should().NotBeNull();
        captured!.Severity.Should().Be("critical");
    }

    // ── Ledger ausente ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WalletWithoutAnyLedgerEntry_CreatesLedgerMissingAlert()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 50, UtcNow.AddDays(-1));

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([wallet]);
        // Sem ledger entries cadastrados para essa wallet.

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType.Should().ContainKey(GoldEconomyAlertTypes.LedgerMissing);
        summary.AlertsCreatedByType[GoldEconomyAlertTypes.LedgerMissing].Should().Be(1);
    }

    // ── RN-002: pedido granted sem débito ───────────────────────────────────────

    [Fact]
    public async Task Handle_GoldOrderGrantedWithoutMatchingDebit_CreatesOrderGrantedWithoutDebitAlert()
    {
        var userId = Guid.NewGuid();
        var order = ShopOrder.Create(userId, "gold", "sku-1", null, null, UtcNow.AddHours(-1));
        order.MarkGranted(UtcNow.AddHours(-1));

        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        // Nenhum ledger entry referenciando esse pedido.

        SecurityAlert? captured = null;
        _securityAlertRepository.Setup(r => r.AddAsync(It.IsAny<SecurityAlert>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAlert, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.OrderGrantedWithoutDebit].Should().Be(1);
        captured!.AffectedUserId.Should().Be(userId);
        captured.Severity.Should().Be("high");
    }

    [Fact]
    public async Task Handle_GoldOrderGrantedWithMatchingDebit_CreatesNoAlert()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 100, UtcNow.AddDays(-1));
        var order = ShopOrder.Create(userId, "gold", "sku-1", null, null, UtcNow.AddHours(-1));
        order.MarkGranted(UtcNow.AddHours(-1));

        var debitEntry = CreateLedgerEntry(
            wallet.Id, GoldLedgerDirection.Debit, 10, 90, UtcNow.AddHours(-1),
            referenceType: "shop_order", referenceId: order.Id.ToString());

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([wallet]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([GetLedgerEntryFromWallet(wallet), debitEntry]);
        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([order]);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType.Should().NotContainKey(GoldEconomyAlertTypes.OrderGrantedWithoutDebit);
    }

    // ── RN-003: crédito sem validação ───────────────────────────────────────────

    [Fact]
    public async Task Handle_CreditReferencingShopOrderNotGranted_CreatesCreditWithoutValidationAlert()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 50, UtcNow.AddDays(-1));
        var order = ShopOrder.Create(userId, "gold", "sku-1", null, null, UtcNow.AddHours(-2));
        // Pedido permanece "pending" (nunca foi marcado granted).

        var creditEntry = CreateLedgerEntry(
            wallet.Id, GoldLedgerDirection.Credit, 50, 50, UtcNow.AddDays(-1),
            referenceType: "shop_order", referenceId: order.Id.ToString());

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([wallet]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([creditEntry]);
        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([order]);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.CreditWithoutValidation].Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Handle_CreditReferencingNonExistentShopOrder_CreatesCreditWithoutValidationAlert()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 50, UtcNow.AddDays(-1));
        var creditEntry = CreateLedgerEntry(
            wallet.Id, GoldLedgerDirection.Credit, 50, 50, UtcNow.AddDays(-1),
            referenceType: "shop_order", referenceId: Guid.NewGuid().ToString());

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([wallet]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([creditEntry]);
        // Nenhum ShopOrder existe.

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.CreditWithoutValidation].Should().BeGreaterThanOrEqualTo(1);
    }

    // ── RN-004: item sem origem rastreável (best-effort) ────────────────────────

    [Fact]
    public async Task Handle_InventoryItemWithoutMatchingGrantedOrder_CreatesItemWithoutOriginAlert()
    {
        var userId = Guid.NewGuid();
        var item = InventoryItem.Create(userId, "sku-amuleto", 1);

        _inventoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);
        // Nenhum ShopOrder granted para esse usuário+produto.

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.ItemWithoutOrigin].Should().Be(1);
    }

    [Fact]
    public async Task Handle_InventoryItemWithMatchingGrantedOrder_CreatesNoAlert()
    {
        var userId = Guid.NewGuid();
        var item = InventoryItem.Create(userId, "sku-amuleto", 1);
        var order = ShopOrder.Create(userId, "gold", "sku-amuleto", null, null, UtcNow.AddHours(-3));
        order.MarkGranted(UtcNow.AddHours(-3));

        _inventoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([item]);
        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([order]);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType.Should().NotContainKey(GoldEconomyAlertTypes.ItemWithoutOrigin);
    }

    [Fact]
    public async Task Handle_InventoryItemWithZeroQuantity_IsIgnored()
    {
        var userId = Guid.NewGuid();
        var item = InventoryItem.Create(userId, "sku-amuleto", 0);

        _inventoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([item]);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType.Should().NotContainKey(GoldEconomyAlertTypes.ItemWithoutOrigin);
    }

    // ── Volume anormal ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserWithMoreThanThresholdOrdersInLast24Hours_CreatesAbnormalVolumeAlert()
    {
        var userId = Guid.NewGuid();
        var orders = Enumerable.Range(0, ReconcileGoldEconomyCommandHandler.AbnormalVolumeThreshold + 1)
            .Select(i => ShopOrder.Create(userId, "gold", "sku-1", null, null, UtcNow.AddMinutes(-i)))
            .ToList();

        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.AbnormalVolume].Should().Be(1);
    }

    [Fact]
    public async Task Handle_UserWithOrdersBelowThreshold_CreatesNoAbnormalVolumeAlert()
    {
        var userId = Guid.NewGuid();
        var orders = Enumerable.Range(0, 3)
            .Select(i => ShopOrder.Create(userId, "gold", "sku-1", null, null, UtcNow.AddMinutes(-i)))
            .ToList();

        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType.Should().NotContainKey(GoldEconomyAlertTypes.AbnormalVolume);
    }

    // ── Muitas falhas de compra ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserWithMoreThanThresholdFailuresInLastHour_CreatesExcessiveFailuresAlert()
    {
        var userId = Guid.NewGuid();
        var orders = Enumerable.Range(0, ReconcileGoldEconomyCommandHandler.ExcessiveFailuresThreshold + 1)
            .Select(i =>
            {
                var o = ShopOrder.Create(userId, "gold", "sku-1", null, null, UtcNow.AddMinutes(-i));
                o.MarkFailed(UtcNow.AddMinutes(-i));
                return o;
            })
            .ToList();

        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.ExcessiveFailures].Should().Be(1);
    }

    // ── Não duplicação de alertas já abertos recentes ──────────────────────────

    [Fact]
    public async Task Handle_TwoGrantedOrdersForSameProductWithinShortWindow_CreatesDuplicatePurchaseAlert()
    {
        var userId = Guid.NewGuid();
        var first = ShopOrder.Create(userId, "gold", "sku-duplicate", null, null, UtcNow.AddSeconds(-8));
        first.MarkGranted(UtcNow.AddSeconds(-8));

        var second = ShopOrder.Create(userId, "gold", "sku-duplicate", null, null, UtcNow.AddSeconds(-3));
        second.MarkGranted(UtcNow.AddSeconds(-3));

        _shopOrderRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreatedByType[GoldEconomyAlertTypes.DuplicatePurchase].Should().Be(1);
    }

    [Fact]
    public async Task Handle_DivergenceAlreadyHasOpenRecentAlert_DoesNotCreateDuplicate()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 100, UtcNow.AddDays(-1));
        var entry = CreateLedgerEntry(wallet.Id, GoldLedgerDirection.Credit, 50, 50, UtcNow.AddDays(-2));

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([wallet]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        _securityAlertRepository
            .Setup(r => r.HasOpenRecentAlertAsync(GoldEconomyAlertTypes.BalanceMismatch, userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var summary = await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        summary.AlertsCreated.Should().Be(0);
        summary.AlertsSkippedAsDuplicate.Should().Be(1);
        _securityAlertRepository.Verify(r => r.AddAsync(It.IsAny<SecurityAlert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Auditoria e persistência ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAlertCreated_RecordsAuditLogWithSystemActorAndSafeMetadata()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 100, UtcNow.AddDays(-1));
        var entry = CreateLedgerEntry(wallet.Id, GoldLedgerDirection.Credit, 50, 50, UtcNow.AddDays(-2));

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([wallet]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.SecurityAlertCreated,
            null,
            AuditActorType.System,
            AuditResourceTypes.SecurityAlert,
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlertCreated_PersistsChangesViaUnitOfWork()
    {
        var userId = Guid.NewGuid();
        var wallet = CreateWallet(userId, 100, UtcNow.AddDays(-1));
        var entry = CreateLedgerEntry(wallet.Id, GoldLedgerDirection.Credit, 50, 50, UtcNow.AddDays(-2));

        _goldWalletRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([wallet]);
        _goldLedgerEntryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoAlertsCreated_DoesNotCallSaveChanges()
    {
        await CreateHandler().Handle(new ReconcileGoldEconomyCommand(), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static GoldLedgerEntry GetLedgerEntryFromWallet(GoldWallet wallet)
    {
        // GoldWallet.Credit já retorna o GoldLedgerEntry correspondente; recriamos aqui
        // de forma equivalente para os testes que só têm a wallet em mãos.
        return CreateLedgerEntry(wallet.Id, GoldLedgerDirection.Credit, wallet.Balance, wallet.Balance, wallet.CreatedAtUtc);
    }

    private static GoldLedgerEntry CreateLedgerEntry(
        Guid walletId,
        GoldLedgerDirection direction,
        long amount,
        long balanceAfter,
        DateTime createdAtUtc,
        string? referenceType = null,
        string? referenceId = null)
    {
        var method = typeof(GoldLedgerEntry).GetMethod(
            "Create",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        return (GoldLedgerEntry)method.Invoke(null, [
            walletId, direction, amount, "test", referenceType, referenceId, balanceAfter, null, createdAtUtc
        ])!;
    }
}
