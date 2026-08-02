using Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEventDetail;
using Awaken.Application.Common.Exceptions;
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
/// US-217 — testes do handler de detalhe seguro de uma validação de assinatura/IAP.
///
/// CA: detalhe não expõe payload sensível (RN-004).
/// CA: evento permite identificar usuário afetado para navegação posterior.
/// </summary>
public class GetSubscriptionEventDetailQueryHandlerTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;

    public GetSubscriptionEventDetailQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeServiceForContext = new Mock<IDateTimeService>();
        dateTimeServiceForContext.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeServiceForContext.Object);
    }

    public void Dispose() => _context.Dispose();

    private GetSubscriptionEventDetailQueryHandler CreateHandler(string environmentName = "Development")
    {
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(h => h.EnvironmentName).Returns(environmentName);

        IAdminSubscriptionDiagnosticsRepository repository =
            new AdminSubscriptionDiagnosticsRepository(_context, hostEnvironment.Object, dateTimeService.Object);

        return new GetSubscriptionEventDetailQueryHandler(repository);
    }

    [Fact]
    public async Task Handle_EventNotFound_ThrowsNotFoundException()
    {
        var act = async () => await CreateHandler().Handle(
            new GetSubscriptionEventDetailQuery(Guid.NewGuid(), "revenuecat_event"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RevenueCatEvent_ReturnsDetailWithoutRawPayload()
    {
        var rcEvent = RevenueCatEvent.Create(
            "evt-1", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "orig-tx-12345678", "plan_monthly",
            payloadHash: "abc123def456");
        _context.RevenueCatEvents.Add(rcEvent);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionEventDetailQuery(rcEvent.Id, "revenuecat_event"), CancellationToken.None);

        result.Id.Should().Be(rcEvent.Id);
        result.Status.Should().Be("approved");
        result.PayloadHashMasked.Should().Be("abc123def456", "PayloadHash já é truncado na origem (US-194), não é payload bruto");
        result.MaskedExternalRef.Should().NotBe("orig-tx-12345678", "referência externa deve ser mascarada (RN-004)");
        result.MaskedExternalRef.Should().EndWith("5678");
    }

    [Fact]
    public async Task Handle_IapLedger_ReturnsDetailWithMaskedTransactionId()
    {
        var userId = Guid.NewGuid();
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-9999888877776666", "gold_pack_small", "google_play", UtcNow);
        ledger.MarkGranted(UtcNow);
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionEventDetailQuery(ledger.Id, "iap_ledger"), CancellationToken.None);

        result.Status.Should().Be("approved");
        result.UserId.Should().Be(userId);
        result.MaskedExternalRef.Should().NotBe("tx-iap-9999888877776666");
        result.MaskedExternalRef.Should().EndWith("6666");
    }

    [Fact]
    public async Task Handle_UserWithMultipleEvents_IncludesRelatedEvents()
    {
        var userId = Guid.NewGuid();
        var firstEvent = RevenueCatEvent.Create(
            "evt-1", userId.ToString(), "INITIAL_PURCHASE", UtcNow.AddDays(-1), "tx-a", "plan_monthly");
        var secondEvent = RevenueCatEvent.Create(
            "evt-2", userId.ToString(), "RENEWAL", UtcNow, "tx-a", "plan_monthly");
        _context.RevenueCatEvents.AddRange(firstEvent, secondEvent);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new GetSubscriptionEventDetailQuery(secondEvent.Id, "revenuecat_event"), CancellationToken.None);

        result.RelatedUserEvents.Should().ContainSingle(e => e.Id == firstEvent.Id,
            "deve listar outros eventos do mesmo usuário para destacar múltiplos eventos (RN-005)");
    }

    [Fact]
    public async Task Handle_InvalidSource_ThrowsNotFoundException()
    {
        var act = async () => await CreateHandler().Handle(
            new GetSubscriptionEventDetailQuery(Guid.NewGuid(), "unknown_source"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
