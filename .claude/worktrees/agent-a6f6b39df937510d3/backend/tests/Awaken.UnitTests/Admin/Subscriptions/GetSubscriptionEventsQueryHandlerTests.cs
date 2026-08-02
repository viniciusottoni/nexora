using Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEvents;
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
/// US-217 — testes do handler de listagem paginada/filtrável de eventos de assinatura/IAP.
///
/// CA: admin consegue filtrar eventos por produto/plano/status/período.
/// CA: validações negadas e pendentes ficam destacadas (via Status na resposta).
/// Cenário de QA: usuário com múltiplos eventos.
/// </summary>
public class GetSubscriptionEventsQueryHandlerTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly AwakenDbContext _context;

    public GetSubscriptionEventsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeServiceForContext = new Mock<IDateTimeService>();
        dateTimeServiceForContext.Setup(d => d.UtcNow).Returns(UtcNow);
        _context = new AwakenDbContext(options, dateTimeServiceForContext.Object);
    }

    public void Dispose() => _context.Dispose();

    private GetSubscriptionEventsQueryHandler CreateHandler(string environmentName = "Development")
    {
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(h => h.EnvironmentName).Returns(environmentName);

        IAdminSubscriptionDiagnosticsRepository repository =
            new AdminSubscriptionDiagnosticsRepository(_context, hostEnvironment.Object, dateTimeService.Object);

        return new GetSubscriptionEventsQueryHandler(repository);
    }

    private static GetSubscriptionEventsQuery Query(
        string? type = null, string? store = null, string? status = null, string? plan = null,
        string? product = null, string? environment = null, Guid? userId = null,
        int page = 1, int pageSize = 20) =>
        new(type, store, status, plan, product, environment, userId, null, null, page, pageSize);

    [Fact]
    public async Task Handle_WhenDatabaseIsEmpty_ReturnsEmptyListWithoutThrowing()
    {
        var act = async () => await CreateHandler().Handle(Query(), CancellationToken.None);

        var result = await act.Should().NotThrowAsync();

        result.Which.Items.Should().BeEmpty();
        result.Which.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_FiltersByStatus_ReturnsOnlyMatchingEvents()
    {
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-approved", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "tx-a", "plan_monthly"));
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-denied", Guid.NewGuid().ToString(), "CANCELLATION", UtcNow, "tx-b", "plan_monthly"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(Query(status: "denied"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().Status.Should().Be("denied");
    }

    [Fact]
    public async Task Handle_FiltersByProduct_ReturnsOnlyMatchingEvents()
    {
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-1", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "tx-a", "plan_monthly"));
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-2", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "tx-b", "plan_annual"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(Query(product: "plan_annual"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().Product.Should().Be("plan_annual");
    }

    [Fact]
    public async Task Handle_FiltersByType_ReturnsOnlyIapEvents()
    {
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-sub", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "tx-a", "plan_monthly"));
        var ledger = IapTransactionLedger.Create(Guid.NewGuid(), "tx-iap", "gold_pack_small", "google_play", UtcNow);
        _context.IapTransactionLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(Query(type: "iap"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().Type.Should().Be("iap");
    }

    [Fact]
    public async Task Handle_UserWithMultipleEvents_ReturnsAllForThatUser()
    {
        var userId = Guid.NewGuid();
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-1", userId.ToString(), "INITIAL_PURCHASE", UtcNow.AddDays(-2), "tx-a", "plan_monthly"));
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-2", userId.ToString(), "RENEWAL", UtcNow.AddDays(-1), "tx-a", "plan_monthly"));
        var ledger = IapTransactionLedger.Create(userId, "tx-iap-user", "gold_pack_small", "google_play", UtcNow);
        _context.IapTransactionLedgers.Add(ledger);
        // Outro usuário não deve aparecer no filtro.
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-other", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "tx-other", "plan_monthly"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(Query(userId: userId), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(i => i.UserId == userId);
    }

    [Fact]
    public async Task Handle_RepeatedTransaction_FlagsIsRepeatedTransaction()
    {
        var appUserId = Guid.NewGuid().ToString();
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-rep-1", appUserId, "INITIAL_PURCHASE", UtcNow.AddMinutes(-10), "orig-tx-repeat", "plan_monthly"));
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-rep-2", appUserId, "RENEWAL", UtcNow, "orig-tx-repeat", "plan_monthly"));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.IsRepeatedTransaction);
    }

    [Fact]
    public async Task Handle_Pagination_RespectsPageAndPageSize()
    {
        for (var i = 0; i < 5; i++)
        {
            _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
                $"evt-{i}", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow.AddMinutes(-i), $"tx-{i}", "plan_monthly"));
        }
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(Query(page: 1, pageSize: 2), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
    }

    [Fact]
    public async Task Handle_EnvironmentFilterMismatch_ReturnsEmpty()
    {
        _context.RevenueCatEvents.Add(RevenueCatEvent.Create(
            "evt-1", Guid.NewGuid().ToString(), "INITIAL_PURCHASE", UtcNow, "tx-a", "plan_monthly"));
        await _context.SaveChangesAsync();

        // Ambiente do processo de teste é "dev" (Development); filtrar por "prod" não deve retornar nada.
        var result = await CreateHandler().Handle(Query(environment: "prod"), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}
