using Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionDiagnostics;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Awaken.UnitTests.Admin.Subscriptions;

/// <summary>
/// US-217 — testes do handler de cards agregados de diagnóstico de assinatura/IAP.
///
/// CA: admin vê volume de validações por status (RN-001 — backend é fonte de verdade).
/// Cenários de QA cobertos: assinatura aprovada, assinatura negada, IAP aprovado,
/// IAP pendente, transação repetida, falha do provider.
/// </summary>
public class GetSubscriptionDiagnosticsQueryHandlerTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;

    public GetSubscriptionDiagnosticsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeServiceForContext = new Mock<IDateTimeService>();
        dateTimeServiceForContext.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeServiceForContext.Object);
    }

    public void Dispose() => _context.Dispose();

    private GetSubscriptionDiagnosticsQueryHandler CreateHandler(string environmentName = "Development")
    {
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(h => h.EnvironmentName).Returns(environmentName);

        IAdminSubscriptionDiagnosticsRepository repository =
            new AdminSubscriptionDiagnosticsRepository(_context, hostEnvironment.Object, dateTimeService.Object);

        return new GetSubscriptionDiagnosticsQueryHandler(repository);
    }

    [Fact]
    public async Task Handle_WhenDatabaseIsEmpty_ReturnsZerosWithoutThrowing()
    {
        var act = async () => await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        var result = await act.Should().NotThrowAsync();

        result.Which.ApprovedCount.Should().Be(0);
        result.Which.DeniedCount.Should().Be(0);
        result.Which.PendingCount.Should().Be(0);
        result.Which.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SubscriptionApproved_CountsAsApproved()
    {
        // Evento de ativação de assinatura (INITIAL_PURCHASE) → approved.
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-1", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "orig-tx-1", "plan_monthly"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        result.ApprovedCount.Should().Be(1);
        result.DeniedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SubscriptionDenied_CountsAsDenied()
    {
        // Evento de expiração/cancelamento → denied.
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-2", Guid.NewGuid().ToString(), "CANCELLATION", UtcNow, "orig-tx-2", "plan_monthly"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        result.DeniedCount.Should().Be(1);
        result.ApprovedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_IapApproved_CountsAsApproved()
    {
        var userId = Guid.NewGuid();
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-1", "gold_pack_small", "google_play", UtcNow);
        ledger.MarkGranted(UtcNow);
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        result.ApprovedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_IapPending_CountsAsPending()
    {
        var userId = Guid.NewGuid();
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-2", "gold_pack_small", "google_play", UtcNow);
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        result.PendingCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ProviderFailure_CountsAsFailed()
    {
        var userId = Guid.NewGuid();
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-3", "gold_pack_small", "google_play", UtcNow);
        ledger.MarkFailed(UtcNow);
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        result.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RepeatedTransaction_CountsInRepeatedTransactions()
    {
        // RN-005: dois eventos com o mesmo OriginalTransactionId → divergência/repetição.
        var appUserId = Guid.NewGuid().ToString();
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-rep-1", appUserId, "INITIAL_PURCHASE", UtcNow.AddMinutes(-10), "orig-tx-repeat", "plan_monthly"));
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-rep-2", appUserId, "RENEWAL", UtcNow, "orig-tx-repeat", "plan_monthly"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null), CancellationToken.None);

        result.RepeatedTransactionsCount.Should().Be(1, "ambos eventos compartilham o mesmo OriginalTransactionId");
    }

    [Fact]
    public async Task Handle_PendingGrantOlderThanThreshold_CountsAsPendingGrant()
    {
        var userId = Guid.NewGuid();
        // Pendente há 60 minutos, threshold de 30 → deve contar.
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-old", "gold_pack_small", "google_play", UtcNow.AddMinutes(-60));
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null, PendingThresholdMinutes: 30), CancellationToken.None);

        result.PendingGrantsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PendingGrantWithinThreshold_DoesNotCountAsPendingGrant()
    {
        var userId = Guid.NewGuid();
        // Pendente há 5 minutos, threshold de 30 → RN-002: ainda não deve contar como atraso.
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-recent", "gold_pack_small", "google_play", UtcNow.AddMinutes(-5));
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionDiagnosticsQuery(null, null, PendingThresholdMinutes: 30), CancellationToken.None);

        result.PendingGrantsCount.Should().Be(0);
        result.PendingCount.Should().Be(1, "ainda deve aparecer como pendente, só não como 'pendente há muito tempo'");
    }
}
