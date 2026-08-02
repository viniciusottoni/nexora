using Awaken.Application.Common.Interfaces;
using Awaken.Application.Economy.Queries.GetTransactions;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Economy;

/// <summary>
/// US-192: testes unitarios do GetTransactionsQueryHandler.
/// CA-001: lista com lancamentos de Gold e pedidos de compra.
/// CA-002: paginacao sem duplicatas.
/// CA-003: sem historico → lista vazia.
/// </summary>
public class GetTransactionsQueryHandlerTests
{
    private readonly Mock<IGoldWalletService> _walletService = new();
    private readonly Mock<IGoldLedgerEntryRepository> _ledgerRepository = new();
    private readonly Mock<IShopOrderRepository> _shopOrderRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private readonly GoldWallet _wallet;

    public GetTransactionsQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        _walletService
            .Setup(s => s.GetOrCreateWalletAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_wallet);
    }

    private GetTransactionsQueryHandler CreateHandler() =>
        new(_walletService.Object, _ledgerRepository.Object, _shopOrderRepository.Object, _currentUserService.Object);

    private void SetupLedger(IReadOnlyList<GoldLedgerEntry> entries)
    {
        _ledgerRepository
            .Setup(r => r.GetPagedByWalletIdAsync(_wallet.Id, 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entries, entries.Count));
    }

    private void SetupOrders(IReadOnlyList<ShopOrder> orders)
    {
        _shopOrderRepository
            .Setup(r => r.GetPagedByUserIdAsync(UserId, 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, orders.Count));
    }

    // ──────────────────────────────────────────────────────────────────────
    // CA-001: lista com lancamentos de Gold e pedidos de compra
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA001_ReturnsGoldMovementsAndShopOrders()
    {
        // Arrange
        var creditEntry = _wallet.Credit(100, "quest_reward", null, null, null, UtcNow);
        var shopOrder = ShopOrder.Create(UserId, "gold", "pedra_dungeon", null, null, UtcNow.AddMinutes(-1));

        SetupLedger([creditEntry]);
        SetupOrders([shopOrder]);

        // Act
        var result = await CreateHandler().Handle(new GetTransactionsQuery(1, 20), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        var goldItem = result.Items.FirstOrDefault(i => i.Type == "gold_movement");
        goldItem.Should().NotBeNull();
        goldItem!.Direction.Should().Be("credit");
        goldItem.Amount.Should().Be(100);
        goldItem.Description.Should().Be("quest_reward");
        goldItem.BalanceAfter.Should().Be(100);

        var orderItem = result.Items.FirstOrDefault(i => i.Type == "shop_order");
        orderItem.Should().NotBeNull();
        orderItem!.Channel.Should().Be("gold");
        orderItem.ProductKey.Should().Be("pedra_dungeon");
        orderItem.Status.Should().Be("pending");
        orderItem.Direction.Should().BeNull();
        orderItem.Amount.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CA001_GoldMovementDebit_HasDebitDirection()
    {
        // Garante que lançamentos de débito são projetados corretamente.
        _wallet.Credit(200, "quest_reward", null, null, null, UtcNow.AddMinutes(-1));
        var debitEntry = _wallet.Debit(50, "shop_purchase", null, null, null, UtcNow);

        SetupLedger([debitEntry]);
        SetupOrders([]);

        var result = await CreateHandler().Handle(new GetTransactionsQuery(1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Direction.Should().Be("debit");
        result.Items[0].Amount.Should().Be(50);
        result.Items[0].BalanceAfter.Should().Be(150);
    }

    // ──────────────────────────────────────────────────────────────────────
    // CA-002: paginação não duplica
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA002_PaginationDoesNotDuplicate()
    {
        // 3 itens no total; pagina 1 com pageSize=2 → 2 itens; pagina 2 → 1 item.
        var e1 = _wallet.Credit(10, "r1", null, null, null, UtcNow.AddMinutes(-2));
        var e2 = _wallet.Credit(20, "r2", null, null, null, UtcNow.AddMinutes(-1));
        var e3 = _wallet.Credit(30, "r3", null, null, null, UtcNow);

        SetupLedger([e1, e2, e3]);
        SetupOrders([]);

        var page1 = await CreateHandler().Handle(new GetTransactionsQuery(1, 2), CancellationToken.None);
        var page2 = await CreateHandler().Handle(new GetTransactionsQuery(2, 2), CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(1);
        page1.HasMore.Should().BeTrue();
        page2.HasMore.Should().BeFalse();
        page1.TotalCount.Should().Be(3);

        // Sem duplicatas entre as páginas.
        var allIds = page1.Items.Select(i => i.Id)
            .Concat(page2.Items.Select(i => i.Id))
            .ToList();
        allIds.Should().OnlyHaveUniqueItems();
    }

    // ──────────────────────────────────────────────────────────────────────
    // CA-003: sem histórico → lista vazia
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CA003_ReturnsEmptyList_WhenNoHistory()
    {
        SetupLedger([]);
        SetupOrders([]);

        var result = await CreateHandler().Handle(new GetTransactionsQuery(1, 20), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }
}
