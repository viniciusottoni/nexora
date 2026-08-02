using Awaken.Application.Admin.Economy.Queries.GetAdminGoldWalletDetail;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Economy;

/// <summary>
/// US-229: testes do GetAdminGoldWalletDetailQueryHandler.
/// CA-001: usuário sem carteira → null (não 404 aqui, controller faz o 404).
/// CA-002: usuário com carteira → saldo e ledger recente.
/// CA-003: ledger retorna entradas com direção correta.
/// </summary>
public class GetAdminGoldWalletDetailQueryHandlerTests
{
    private readonly Mock<IGoldWalletRepository>      _wallets = new();
    private readonly Mock<IGoldLedgerEntryRepository> _ledger  = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc);

    private GetAdminGoldWalletDetailQueryHandler CreateHandler() =>
        new(_wallets.Object, _ledger.Object);

    // CA-001: usuário sem carteira → null
    [Fact]
    public async Task Handle_CA001_NoWallet_ReturnsNull()
    {
        _wallets.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var result = await CreateHandler().Handle(
            new GetAdminGoldWalletDetailQuery(UserId), CancellationToken.None);

        result.Should().BeNull();
    }

    // CA-002: carteira com saldo e ledger
    [Fact]
    public async Task Handle_CA002_WalletExists_ReturnsSummary()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, Now.AddDays(-5));
        var credit = wallet.Credit(200, "gold_purchase", null, null, null, Now.AddDays(-4));
        var debit  = wallet.Debit(50, "shop_purchase", null, null, null, Now.AddDays(-1));

        _wallets.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _ledger.Setup(r => r.GetPagedByWalletIdAsync(wallet.Id, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { debit, credit } as IReadOnlyList<GoldLedgerEntry>, 2));

        var result = await CreateHandler().Handle(
            new GetAdminGoldWalletDetailQuery(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(UserId);
        result.Balance.Should().Be(wallet.Balance);
        result.TotalLedgerEntries.Should().Be(2);
        result.RecentLedger.Should().HaveCount(2);
    }

    // CA-003: direções projetadas corretamente
    [Fact]
    public async Task Handle_CA003_LedgerDirections_ProjectedCorrectly()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, Now);
        var credit = wallet.Credit(100, "gold_purchase", null, null, null, Now.AddHours(-2));
        var debit  = wallet.Debit(30, "shop_purchase", null, null, null, Now.AddHours(-1));

        _wallets.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _ledger.Setup(r => r.GetPagedByWalletIdAsync(wallet.Id, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { debit, credit } as IReadOnlyList<GoldLedgerEntry>, 2));

        var result = await CreateHandler().Handle(
            new GetAdminGoldWalletDetailQuery(UserId), CancellationToken.None);

        result!.RecentLedger.Should().Contain(e => e.Direction == "credit" && e.Amount == 100);
        result.RecentLedger.Should().Contain(e => e.Direction == "debit" && e.Amount == 30);
    }
}
