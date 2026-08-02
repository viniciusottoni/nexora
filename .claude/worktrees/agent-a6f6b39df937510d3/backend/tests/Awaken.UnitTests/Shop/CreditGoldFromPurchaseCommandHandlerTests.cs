using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Commands.CreditGoldFromPurchase;
using Awaken.Domain.Entities.Economy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// Testes unitários para CreditGoldFromPurchaseCommandHandler (US-226).
/// RN-001/RN-002: o handler apenas executa o crédito de um valor já resolvido
/// pelo chamador — não há nenhuma lógica de resolução de quantidade aqui.
/// </summary>
public class CreditGoldFromPurchaseCommandHandlerTests
{
    private readonly Mock<IGoldWalletService> _goldWalletService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ShopOrderId = Guid.NewGuid();

    private CreditGoldFromPurchaseCommandHandler CreateHandler() =>
        new(_goldWalletService.Object, NullLogger<CreditGoldFromPurchaseCommandHandler>.Instance);

    [Fact]
    public async Task Handle_CreditsWalletWithExactAmountFromCommand()
    {
        // Arrange
        const long amount = 500;
        _goldWalletService
            .Setup(s => s.CreditAsync(
                UserId, amount, "gold_purchase", "shop_order", ShopOrderId.ToString(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLedgerEntry(amount));

        var command = new CreditGoldFromPurchaseCommand(UserId, amount, ShopOrderId);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — crédito delegado ao GoldWalletService com o valor exato do comando.
        _goldWalletService.Verify(s => s.CreditAsync(
            UserId, amount, "gold_purchase", "shop_order", ShopOrderId.ToString(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesShopOrderReferenceForTraceability()
    {
        // Arrange — RN-007: o crédito deve ser rastreável até o ShopOrder de origem.
        _goldWalletService
            .Setup(s => s.CreditAsync(
                It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLedgerEntry(100));

        var command = new CreditGoldFromPurchaseCommand(UserId, 100, ShopOrderId);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        _goldWalletService.Verify(s => s.CreditAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(),
            "shop_order", ShopOrderId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static GoldLedgerEntry CreateLedgerEntry(long amount)
    {
        var wallet = GoldWallet.CreateEmpty(UserId, DateTime.UtcNow);
        return wallet.Credit(amount, "gold_purchase", "shop_order", ShopOrderId.ToString(), null, DateTime.UtcNow);
    }
}
