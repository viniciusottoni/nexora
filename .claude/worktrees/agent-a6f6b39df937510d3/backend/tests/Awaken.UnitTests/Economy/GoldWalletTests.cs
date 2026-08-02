using Awaken.Domain.Entities.Economy;
using FluentAssertions;

namespace Awaken.UnitTests.Economy;

public class GoldWalletTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateEmpty_StartsWithZeroBalance()
    {
        // CA-001: usuario que nunca teve carteira deve ter saldo 0.
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);

        wallet.UserId.Should().Be(UserId);
        wallet.Balance.Should().Be(0);
    }

    [Fact]
    public void Credit_IncreasesBalance_AndReturnsLedgerEntryWithBalanceAfter()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);

        var entry = wallet.Credit(100, "quest_reward", "Quest", Guid.NewGuid().ToString(), "corr-1", UtcNow);

        wallet.Balance.Should().Be(100);
        entry.Direction.Should().Be(GoldLedgerDirection.Credit);
        entry.Amount.Should().Be(100);
        entry.BalanceAfter.Should().Be(100);
        entry.WalletId.Should().Be(wallet.Id);
        entry.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void Debit_DecreasesBalance_WhenSufficientFunds()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(100, "quest_reward", null, null, null, UtcNow);

        var entry = wallet.Debit(40, "shop_purchase", "ShopOrder", Guid.NewGuid().ToString(), "corr-2", UtcNow);

        wallet.Balance.Should().Be(60);
        entry.Direction.Should().Be(GoldLedgerDirection.Debit);
        entry.Amount.Should().Be(40);
        entry.BalanceAfter.Should().Be(60);
    }

    [Fact]
    public void Debit_Throws_AndDoesNotChangeBalance_WhenInsufficientFunds()
    {
        // CA-002: debito com saldo insuficiente deve falhar sem gravar nada.
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(10, "quest_reward", null, null, null, UtcNow);

        var act = () => wallet.Debit(11, "shop_purchase", null, null, null, UtcNow);

        act.Should().Throw<InsufficientGoldBalanceException>();
        wallet.Balance.Should().Be(10);
    }

    [Fact]
    public void Debit_Throws_WhenBalanceIsZero()
    {
        // CA-002: usuario sem nenhum credito (saldo 0) nao pode debitar.
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);

        var act = () => wallet.Debit(1, "shop_purchase", null, null, null, UtcNow);

        act.Should().Throw<InsufficientGoldBalanceException>();
        wallet.Balance.Should().Be(0);
    }

    [Fact]
    public void Credit_Throws_WhenAmountIsNotPositive()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);

        var act = () => wallet.Credit(0, "quest_reward", null, null, null, UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Debit_Throws_WhenAmountIsNotPositive()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(10, "quest_reward", null, null, null, UtcNow);

        var act = () => wallet.Debit(-1, "shop_purchase", null, null, null, UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BalanceAfterSeriesOfMovements_MatchesSumOfLedgerEntries()
    {
        // CA-003: o saldo deve ser reconciliavel pela soma do ledger.
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        var entries = new List<GoldLedgerEntry>
        {
            wallet.Credit(100, "quest_reward", null, null, null, UtcNow),
            wallet.Credit(50, "streak_bonus", null, null, null, UtcNow),
            wallet.Debit(30, "shop_purchase", null, null, null, UtcNow),
            wallet.Debit(20, "shop_purchase", null, null, null, UtcNow),
        };

        var reconciledBalance = entries.Sum(e =>
            e.Direction == GoldLedgerDirection.Credit ? e.Amount : -e.Amount);

        reconciledBalance.Should().Be(wallet.Balance);
        entries.Last().BalanceAfter.Should().Be(wallet.Balance);
    }
}
