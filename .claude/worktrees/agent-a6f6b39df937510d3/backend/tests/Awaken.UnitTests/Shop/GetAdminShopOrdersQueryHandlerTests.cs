using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Queries.GetAdminShopOrders;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// US-193 — Testes unitários do handler de consulta administrativa de pedidos.
///
/// CA-001: não-admin → RBAC nega no controller ([Authorize(Policy="Admin")]).
///         No handler, testamos que o handler em si responde corretamente quando invocado
///         (a restrição de acesso é responsabilidade do ASP.NET Core policy; o handler
///         não repete a guarda — ver CA-001 em IntegrationTests para o teste E2E real).
///
/// CA-002: admin vê canal, status, datas e CorrelationId — sem dado de pagamento.
///
/// CA-003: cada consulta administrativa gera AuditLog com ActorType = Admin.
/// </summary>
public class GetAdminShopOrdersQueryHandlerTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IShopOrderRepository> _shopOrderRepo = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();

    public GetAdminShopOrdersQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(AdminUserId);
        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "corr-admin-test-001";
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }

    private GetAdminShopOrdersQueryHandler CreateHandler() =>
        new(_shopOrderRepo.Object, _currentUserService.Object,
            _auditLogService.Object, _unitOfWork.Object, _httpContextAccessor.Object);

    private ShopOrder CreateOrder(Guid? userId = null, string channel = "gold",
        string productKey = "reforja_scroll", string status = "granted",
        string? externalTransactionId = null, string? correlationId = "corr-order-001")
    {
        var order = ShopOrder.Create(
            userId ?? Guid.NewGuid(),
            channel,
            productKey,
            externalTransactionId,
            correlationId,
            UtcNow);

        if (status == "granted") order.MarkGranted(UtcNow);
        if (status == "failed") order.MarkFailed(UtcNow);

        return order;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CA-002: admin vê canal, status, datas e CorrelationId — sem token
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_WhenOrdersExist_ReturnsProjectedData()
    {
        // CA-002: admin vê os campos esperados.
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, "gold", "reforja_scroll", "granted");

        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder> { order }, 1));

        var result = await CreateHandler().Handle(new GetAdminShopOrdersQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Channel.Should().Be("gold");
        item.ProductKey.Should().Be("reforja_scroll");
        item.Status.Should().Be("granted");
        item.UserId.Should().Be(userId);
        item.GrantedAtUtc.Should().Be(UtcNow);
        item.CorrelationId.Should().Be("corr-order-001");
    }

    [Fact]
    public async Task Handler_ResponseDoesNotContainPaymentToken()
    {
        // CA-002 / RN-003: ExternalTransactionId é ID de referência, não dado de pagamento.
        // O response deve expor o campo para rastreabilidade, mas metadados de auditoria
        // não devem conter dados sensíveis (ADR-015).
        const string txnId = "TXN-SENSITIVE-999";
        var order = CreateOrder(externalTransactionId: txnId);

        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder> { order }, 1));

        // Capturar metadados de auditoria — não devem conter o TransactionId.
        string? capturedMetadata = null;
        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, _, _, _, meta, _) => capturedMetadata = meta)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(new GetAdminShopOrdersQuery(), CancellationToken.None);

        // ExternalTransactionId pode aparecer no response (é ID de referência, não token).
        result.Items[0].ExternalTransactionId.Should().Be(txnId);

        // Mas os metadados de auditoria não devem conter o TransactionId (ADR-015).
        capturedMetadata.Should().NotContain(txnId,
            "metadados de auditoria não devem conter ExternalTransactionId (ADR-015)");
        capturedMetadata.Should().NotContainAny(
            new[] { "token", "Token", "TOKEN", "receipt", "payment", "Payment" },
            "metadados não devem conter dados de pagamento (ADR-015)");
    }

    [Fact]
    public async Task Handler_PaginationMetadata_IsCorrect()
    {
        // Paginação: page=2, pageSize=5, totalCount=12 → hasMore=true.
        var orders = Enumerable.Range(0, 5)
            .Select(_ => CreateOrder())
            .ToList();

        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 12));

        var result = await CreateHandler()
            .Handle(new GetAdminShopOrdersQuery(Page: 2, PageSize: 5), CancellationToken.None);

        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().Be(12);
        result.HasMore.Should().BeTrue("page=2 * pageSize=5 = 10 < 12 total");
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handler_WhenLastPage_HasMoreIsFalse()
    {
        var orders = Enumerable.Range(0, 2).Select(_ => CreateOrder()).ToList();

        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 3, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 12)); // page 3 * 5 = 15 >= 12

        var result = await CreateHandler()
            .Handle(new GetAdminShopOrdersQuery(Page: 3, PageSize: 5), CancellationToken.None);

        result.HasMore.Should().BeFalse("page=3 * pageSize=5 = 15 >= 12 total");
    }

    [Fact]
    public async Task Handler_PassesFiltersToRepository()
    {
        // Verifica que o handler repassa os filtros corretamente para o repositório.
        var userId = Guid.NewGuid();
        var dateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                userId, "granted", "gold", dateFrom, dateTo, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder>(), 0));

        var query = new GetAdminShopOrdersQuery(
            UserId: userId,
            Status: "granted",
            Channel: "gold",
            DateFrom: dateFrom,
            DateTo: dateTo,
            Page: 1,
            PageSize: 10);

        await CreateHandler().Handle(query, CancellationToken.None);

        _shopOrderRepo.Verify(r => r.GetPagedByFilterAsync(
            userId, "granted", "gold", dateFrom, dateTo, 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CA-003: auditoria admin gerada com ActorType = Admin
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_WhenHandled_RecordsAuditWithActorTypeAdmin()
    {
        // CA-003: acesso admin gera AuditLog com ActorType = Admin.
        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder>(), 0));

        AuditActorType? capturedActorType = null;
        string? capturedAction = null;
        Guid? capturedActorUserId = null;
        string? capturedResourceType = null;

        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (action, actorUserId, actorType, resourceType, _, _, _) =>
                {
                    capturedAction = action;
                    capturedActorUserId = actorUserId;
                    capturedActorType = actorType;
                    capturedResourceType = resourceType;
                })
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(new GetAdminShopOrdersQuery(), CancellationToken.None);

        capturedAction.Should().Be(AuditActions.AdminShopOrdersQueried,
            "CA-003: ação deve ser AdminShopOrders.Queried");
        capturedActorType.Should().Be(AuditActorType.Admin,
            "CA-003: ActorType deve ser Admin");
        capturedActorUserId.Should().Be(AdminUserId,
            "CA-003: ActorUserId deve ser o do admin autenticado");
        capturedResourceType.Should().Be(AuditResourceTypes.ShopOrder,
            "CA-003: ResourceType deve ser ShopOrder");
    }

    [Fact]
    public async Task Handler_AuditIsCalledExactlyOnce_PerQuery()
    {
        // CA-003: exatamente uma entrada de auditoria por consulta.
        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder>(), 0));

        await CreateHandler().Handle(new GetAdminShopOrdersQuery(), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminShopOrdersQueried,
            AdminUserId,
            AuditActorType.Admin,
            AuditResourceTypes.ShopOrder,
            null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_WhenAuditFails_QueryStillSucceeds()
    {
        // Falha de auditoria não cancela a consulta (princípio de resiliência — RN-005).
        var order = CreateOrder();
        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder> { order }, 1));

        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Audit storage indisponível"));

        var act = () => CreateHandler().Handle(new GetAdminShopOrdersQuery(), CancellationToken.None);

        var result = await act.Should().NotThrowAsync(
            "falha de auditoria não deve cancelar a consulta administrativa");

        result.Which.Items.Should().HaveCount(1,
            "resultado deve ser retornado mesmo quando a auditoria falha");
    }

    [Fact]
    public async Task Handler_EmptyResult_ReturnsEmptyPagedResponse()
    {
        _shopOrderRepo
            .Setup(r => r.GetPagedByFilterAsync(
                null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopOrder>(), 0));

        var result = await CreateHandler().Handle(new GetAdminShopOrdersQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }
}
