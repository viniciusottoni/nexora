using Awaken.Application.Admin.Economy.Queries.GetGoldEconomySummary;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Economy;

/// <summary>
/// US-229: testes do GetGoldEconomySummaryQueryHandler.
/// CA-001: sem dados → indicadores zerados.
/// CA-002: créditos e débitos são somados corretamente.
/// CA-003: pedidos gold são contados por status.
/// CA-004: alertas gold abertos são contados; não sensíveis.
/// CA-005: período default é 30 dias a partir de utcNow.
/// </summary>
public class GetGoldEconomySummaryQueryHandlerTests
{
    private readonly Mock<IGoldWalletRepository>      _wallets  = new();
    private readonly Mock<IGoldLedgerEntryRepository> _ledger   = new();
    private readonly Mock<IShopOrderRepository>       _orders   = new();
    private readonly Mock<ISecurityAlertRepository>   _alerts   = new();
    private readonly Mock<IDateTimeService>            _clock    = new();

    private static readonly DateTime UtcNow = new(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId1 = Guid.NewGuid();
    private static readonly Guid UserId2 = Guid.NewGuid();

    public GetGoldEconomySummaryQueryHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(UtcNow);

        // Default: empty collections
        _wallets.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GoldWallet>());
        _ledger.Setup(r => r.GetAdminPagedAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<GoldLedgerEntry>(), 0));
        _orders.Setup(r => r.GetPagedByFilterAsync(null, null, "gold", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ShopOrder>(), 0));
        _alerts.Setup(r => r.GetPagedAsync(null, null, "open", null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<SecurityAlert>(), 0));
        _alerts.Setup(r => r.GetPagedAsync(GoldEconomyAlertTypes.AbnormalVolume, null, "open", null, 1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<SecurityAlert>(), 0));
        _alerts.Setup(r => r.GetPagedAsync(null, null, null, null, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<SecurityAlert>(), 0));
    }

    private GetGoldEconomySummaryQueryHandler CreateHandler() =>
        new(_wallets.Object, _ledger.Object, _orders.Object, _alerts.Object, _clock.Object);

    // ──────────────────────────────────────────────────────────────────────────
    // CA-001: sem dados → indicadores zerados
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA001_EmptyData_ReturnsZeroedIndicators()
    {
        var result = await CreateHandler().Handle(
            new GetGoldEconomySummaryQuery(null, null), CancellationToken.None);

        result.TotalGoldPurchased.Should().Be(0);
        result.TotalGoldSpent.Should().Be(0);
        result.TotalInCirculation.Should().Be(0);
        result.OrdersGranted.Should().Be(0);
        result.OrdersPending.Should().Be(0);
        result.OrdersFailed.Should().Be(0);
        result.OpenGoldAlerts.Should().Be(0);
        result.TopProducts.Should().BeEmpty();
        result.AbnormalUsers.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CA-002: créditos e débitos somados corretamente
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA002_SumsCreditAndDebitCorrectly()
    {
        var wallet1 = GoldWallet.CreateEmpty(UserId1, UtcNow);
        wallet1.Credit(500, "gold_purchase", null, null, null, UtcNow);
        var debit = wallet1.Debit(200, "shop_purchase", null, null, null, UtcNow);

        var entries = new[] { wallet1.Credit(500, "gold_purchase", null, null, null, UtcNow.AddMinutes(-5)), debit };

        // wallet balance = 500 + 500 - 200 = 800
        var wallets = new[] { wallet1 };
        _wallets.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallets);

        // ledger: two credits (500 each) + one debit (200)
        var credit1 = wallet1.Credit(100, "gold_purchase", null, null, null, UtcNow.AddHours(-2));
        var credit2 = wallet1.Credit(400, "gold_purchase", null, null, null, UtcNow.AddHours(-1));
        var debit1  = wallet1.Debit(50, "shop_purchase", null, null, null, UtcNow);

        _ledger.Setup(r => r.GetAdminPagedAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { credit1, credit2, debit1 } as IReadOnlyList<GoldLedgerEntry>, 3));

        var result = await CreateHandler().Handle(
            new GetGoldEconomySummaryQuery(null, null), CancellationToken.None);

        result.TotalGoldPurchased.Should().Be(500); // credit1 + credit2
        result.TotalGoldSpent.Should().Be(50);      // debit1
        result.TotalInCirculation.Should().Be(wallet1.Balance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CA-003: pedidos por status contados corretamente
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA003_CountsOrdersByStatus()
    {
        var orderGranted = ShopOrder.Create(UserId1, "gold", "item_a", null, null, UtcNow.AddHours(-3));
        orderGranted.MarkGranted(UtcNow.AddHours(-2));
        var orderPending = ShopOrder.Create(UserId1, "gold", "item_b", null, null, UtcNow.AddHours(-1));
        var orderFailed  = ShopOrder.Create(UserId2, "gold", "item_c", null, null, UtcNow.AddMinutes(-30));
        orderFailed.MarkFailed(UtcNow.AddMinutes(-20));

        _orders.Setup(r => r.GetPagedByFilterAsync(null, null, "gold", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { orderGranted, orderPending, orderFailed } as IReadOnlyList<ShopOrder>, 3));

        var result = await CreateHandler().Handle(
            new GetGoldEconomySummaryQuery(null, null), CancellationToken.None);

        result.OrdersGranted.Should().Be(1);
        result.OrdersPending.Should().Be(1);
        result.OrdersFailed.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CA-004: alertas gold abertos contados; alertas de outro tipo excluídos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA004_CountsOnlyGoldAlerts()
    {
        var goldAlert = SecurityAlert.Create(
            GoldEconomyAlertTypes.BalanceMismatch, "high", "prod", UtcNow.AddHours(-1));
        var otherAlert = SecurityAlert.Create(
            "brute_force", "critical", "prod", UtcNow.AddHours(-1));

        _alerts.Setup(r => r.GetPagedAsync(null, null, "open", null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { goldAlert, otherAlert } as IReadOnlyList<SecurityAlert>, 2));

        var result = await CreateHandler().Handle(
            new GetGoldEconomySummaryQuery(null, null), CancellationToken.None);

        result.OpenGoldAlerts.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CA-005: período default = últimos 30 dias
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA005_DefaultPeriodIs30Days()
    {
        var result = await CreateHandler().Handle(
            new GetGoldEconomySummaryQuery(null, null), CancellationToken.None);

        result.FromUtc.Should().BeCloseTo(UtcNow.AddDays(-30), TimeSpan.FromSeconds(5));
        result.ToUtc.Should().BeCloseTo(UtcNow, TimeSpan.FromSeconds(5));
    }
}
