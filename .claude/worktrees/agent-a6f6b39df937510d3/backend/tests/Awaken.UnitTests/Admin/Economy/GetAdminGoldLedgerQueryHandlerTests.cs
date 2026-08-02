using Awaken.Application.Admin.Economy.Queries.GetAdminGoldLedger;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Economy;

/// <summary>
/// US-229: testes do GetAdminGoldLedgerQueryHandler.
/// CA-001: sem userId → retorna todos os lançamentos paginados.
/// CA-002: userId inválido (sem carteira) → lista vazia.
/// CA-003: userId válido → apenas lançamentos desta carteira.
/// CA-004: direção é projetada como "credit"/"debit".
/// </summary>
public class GetAdminGoldLedgerQueryHandlerTests
{
    private readonly Mock<IGoldWalletRepository>      _wallets = new();
    private readonly Mock<IGoldLedgerEntryRepository> _ledger  = new();

    private static readonly Guid UserId   = Guid.NewGuid();
    private static readonly DateTime Now  = new(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc);

    private GetAdminGoldLedgerQueryHandler CreateHandler() =>
        new(_wallets.Object, _ledger.Object);

    // CA-001: sem userId → delega para repositório sem walletId
    [Fact]
    public async Task Handle_CA001_NoUserId_ReturnsAllEntries()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, Now);
        var credit = wallet.Credit(100, "quest_reward", null, null, null, Now);

        _wallets.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { wallet });
        _ledger.Setup(r => r.GetAdminPagedAsync(null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { credit } as IReadOnlyList<GoldLedgerEntry>, 1));

        var result = await CreateHandler().Handle(
            new GetAdminGoldLedgerQuery(null, null, null, null, 1, 20),
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Direction.Should().Be("credit");
        result.Items[0].Amount.Should().Be(100);
    }

    // CA-002: userId sem carteira → lista vazia (não 404)
    [Fact]
    public async Task Handle_CA002_UserIdWithNoWallet_ReturnsEmpty()
    {
        _wallets.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var result = await CreateHandler().Handle(
            new GetAdminGoldLedgerQuery(UserId, null, null, null, 1, 20),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    // CA-003: userId válido → walletId passado ao repositório
    [Fact]
    public async Task Handle_CA003_ValidUserId_FiltersToWallet()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, Now);
        var credit = wallet.Credit(50, "gold_purchase", null, null, null, Now);

        _wallets.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _ledger.Setup(r => r.GetAdminPagedAsync(wallet.Id, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { credit } as IReadOnlyList<GoldLedgerEntry>, 1));

        var result = await CreateHandler().Handle(
            new GetAdminGoldLedgerQuery(UserId, null, null, null, 1, 20),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].UserId.Should().Be(UserId);
    }

    // CA-004: debit é projetado como "debit"
    [Fact]
    public async Task Handle_CA004_DebitEntry_ProjectedCorrectly()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, Now);
        wallet.Credit(200, "gold_purchase", null, null, null, Now.AddMinutes(-10));
        var debit = wallet.Debit(80, "shop_purchase", "shop_order", "order-id", null, Now);

        _wallets.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _ledger.Setup(r => r.GetAdminPagedAsync(wallet.Id, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { debit } as IReadOnlyList<GoldLedgerEntry>, 1));

        var result = await CreateHandler().Handle(
            new GetAdminGoldLedgerQuery(UserId, null, null, null, 1, 20),
            CancellationToken.None);

        result.Items[0].Direction.Should().Be("debit");
        result.Items[0].ReferenceType.Should().Be("shop_order");
    }
}
