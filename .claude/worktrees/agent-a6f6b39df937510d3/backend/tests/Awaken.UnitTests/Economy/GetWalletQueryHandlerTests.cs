using Awaken.Application.Common.Interfaces;
using Awaken.Application.Economy.Queries.GetWallet;
using Awaken.Domain.Entities.Economy;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Economy;

public class GetWalletQueryHandlerTests
{
    private readonly Mock<IGoldWalletService> _walletService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    public GetWalletQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetWalletQueryHandler CreateHandler() => new(_walletService.Object, _currentUserService.Object);

    [Fact]
    public async Task Handle_ReturnsZeroBalance_WhenWalletIsCreatedLazily()
    {
        // CA-001: saldo inicial deve ser 0 sem erro, criando a carteira.
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        _walletService
            .Setup(s => s.GetOrCreateWalletAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var result = await CreateHandler().Handle(new GetWalletQuery(), CancellationToken.None);

        result.Balance.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsCurrentBalance_WhenWalletAlreadyHasMovements()
    {
        var wallet = GoldWallet.CreateEmpty(UserId, UtcNow);
        wallet.Credit(50, "quest_reward", null, null, null, UtcNow);
        _walletService
            .Setup(s => s.GetOrCreateWalletAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var result = await CreateHandler().Handle(new GetWalletQuery(), CancellationToken.None);

        result.Balance.Should().Be(50);
    }
}
