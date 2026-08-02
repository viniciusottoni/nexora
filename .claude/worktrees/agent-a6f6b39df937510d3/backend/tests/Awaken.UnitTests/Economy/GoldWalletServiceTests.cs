using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Economy;

public class GoldWalletServiceTests
{
    private readonly Mock<IGoldWalletRepository> _walletRepository = new();
    private readonly Mock<IGoldLedgerEntryRepository> _ledgerRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    public GoldWalletServiceTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Awaken.Domain.Entities.Audit.AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private GoldWalletService CreateService() =>
        new(
            _walletRepository.Object,
            _ledgerRepository.Object,
            _dateTimeService.Object,
            _unitOfWork.Object,
            _httpContextAccessor.Object,
            _auditLogService.Object,
            NullLogger<GoldWalletService>.Instance
        );

    [Fact]
    public async Task GetOrCreateWalletAsync_CreatesWalletWithZeroBalance_WhenUserNeverHadOne()
    {
        // CA-001: usuario que nunca teve carteira deve ter saldo 0 criado na primeira consulta.
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var wallet = await CreateService().GetOrCreateWalletAsync(UserId, CancellationToken.None);

        wallet.Balance.Should().Be(0);
        wallet.UserId.Should().Be(UserId);
        _walletRepository.Verify(r => r.AddAsync(It.IsAny<GoldWallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateWalletAsync_ReturnsExistingWallet_WithoutCreatingAnother()
    {
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var wallet = await CreateService().GetOrCreateWalletAsync(UserId, CancellationToken.None);

        wallet.Should().BeSameAs(existing);
        _walletRepository.Verify(r => r.AddAsync(It.IsAny<GoldWallet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditAsync_IncreasesBalance_AndPersistsLedgerEntry()
    {
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var entry = await CreateService().CreditAsync(UserId, 100, "quest_reward", cancellationToken: CancellationToken.None);

        entry.Amount.Should().Be(100);
        entry.BalanceAfter.Should().Be(100);
        existing.Balance.Should().Be(100);
        _walletRepository.Verify(r => r.Update(existing), Times.Once);
        _ledgerRepository.Verify(r => r.AddAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DebitAsync_DecreasesBalance_WhenSufficientFunds()
    {
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        existing.Credit(100, "quest_reward", null, null, null, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var entry = await CreateService().DebitAsync(UserId, 30, "shop_purchase", cancellationToken: CancellationToken.None);

        entry.Amount.Should().Be(30);
        entry.BalanceAfter.Should().Be(70);
        existing.Balance.Should().Be(70);
    }

    [Fact]
    public async Task DebitAsync_Throws_AndDoesNotPersistLedgerEntry_WhenInsufficientFunds()
    {
        // CA-002: debito com saldo insuficiente deve falhar sem gravar lancamento.
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        existing.Credit(10, "quest_reward", null, null, null, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = async () =>
            await CreateService().DebitAsync(UserId, 50, "shop_purchase", cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientGoldBalanceException>();
        existing.Balance.Should().Be(10);
        _ledgerRepository.Verify(
            r => r.AddAsync(It.IsAny<GoldLedgerEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _walletRepository.Verify(r => r.Update(It.IsAny<GoldWallet>()), Times.Never);
    }

    [Fact]
    public async Task DebitAsync_Throws_WhenWalletWasNeverCredited()
    {
        // CA-002: usuario com carteira recem-criada (saldo 0) nao pode debitar.
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var act = async () =>
            await CreateService().DebitAsync(UserId, 1, "shop_purchase", cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientGoldBalanceException>();
    }

    // ─── US-227 / RN-004 / RN-005: retry de concorrencia otimista ─────────────

    [Fact]
    public async Task DebitAsync_RetriesAndSucceeds_WhenConcurrencyConflictOccursOnce()
    {
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        existing.Credit(100, "quest_reward", null, null, null, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Simula o efeito real do EF Core Entry(wallet).ReloadAsync(): restaura
        // o saldo para o valor persistido no banco (sem o debito da tentativa
        // que falhou), permitindo que a proxima tentativa recalcule do zero.
        _walletRepository
            .Setup(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => SetBalance(existing, 100))
            .Returns(Task.CompletedTask);

        var saveAttempt = 0;
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                saveAttempt++;
                if (saveAttempt == 1)
                    throw new DbUpdateConcurrencyException("conflito simulado");

                return Task.FromResult(1);
            });

        var entry = await CreateService().DebitAsync(
            UserId, 30, "shop_purchase", cancellationToken: CancellationToken.None);

        entry.Amount.Should().Be(30);
        entry.BalanceAfter.Should().Be(70);
        existing.Balance.Should().Be(70);
        saveAttempt.Should().Be(2);
        _walletRepository.Verify(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// Simula a restauracao de estado feita por context.Entry(wallet).ReloadAsync()
    /// no EF Core real, onde as propriedades escalares (Balance) sao
    /// sobrescritas com o valor persistido no banco.
    private static void SetBalance(GoldWallet wallet, long balance) =>
        typeof(GoldWallet).GetProperty(nameof(GoldWallet.Balance))!
            .SetValue(wallet, balance);

    [Fact]
    public async Task DebitAsync_ThrowsConcurrencyConflictException_WhenRetriesExhausted()
    {
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        existing.Credit(100, "quest_reward", null, null, null, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _walletRepository
            .Setup(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => SetBalance(existing, 100))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("conflito simulado"));

        var act = async () =>
            await CreateService().DebitAsync(UserId, 30, "shop_purchase", cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        _walletRepository.Verify(
            r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreditAsync_RetriesAndSucceeds_WhenConcurrencyConflictOccursOnce()
    {
        var existing = GoldWallet.CreateEmpty(UserId, UtcNow);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Simula o reload do EF Core restaurando o saldo persistido (0) antes
        // da segunda tentativa, evitando duplo credito em memoria.
        _walletRepository
            .Setup(r => r.ReloadAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => SetBalance(existing, 0))
            .Returns(Task.CompletedTask);

        var saveAttempt = 0;
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                saveAttempt++;
                if (saveAttempt == 1)
                    throw new DbUpdateConcurrencyException("conflito simulado");

                return Task.FromResult(1);
            });

        var entry = await CreateService().CreditAsync(
            UserId, 100, "quest_reward", cancellationToken: CancellationToken.None);

        entry.BalanceAfter.Should().Be(100);
        existing.Balance.Should().Be(100);
        saveAttempt.Should().Be(2);
    }
}
