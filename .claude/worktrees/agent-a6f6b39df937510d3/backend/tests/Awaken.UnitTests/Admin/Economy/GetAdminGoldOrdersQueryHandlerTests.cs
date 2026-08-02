using Awaken.Application.Admin.Economy.Queries.GetAdminGoldOrders;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Economy;

/// <summary>
/// US-229: testes do GetAdminGoldOrdersQueryHandler.
/// CA-001: retorna apenas pedidos channel="gold".
/// CA-002: filtro de status aplicado pelo repositório.
/// CA-003: filtro de ProductKey aplicado em memória (contém, case-insensitive).
/// CA-004: paginação correta quando ProductKey filtra em memória.
/// CA-005: ExternalTransactionId NÃO aparece na resposta (RN-003).
/// </summary>
public class GetAdminGoldOrdersQueryHandlerTests
{
    private readonly Mock<IShopOrderRepository> _orders = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc);

    private GetAdminGoldOrdersQueryHandler CreateHandler() =>
        new(_orders.Object);

    private static ShopOrder MakeOrder(string productKey, string status = "granted", string? externalId = null)
    {
        var o = ShopOrder.Create(UserId, "gold", productKey, externalId, null, Now.AddHours(-1));
        if (status == "granted") o.MarkGranted(Now);
        if (status == "failed")  o.MarkFailed(Now);
        return o;
    }

    // CA-001: retorna apenas pedidos channel=gold (repositório filtra pelo canal)
    [Fact]
    public async Task Handle_CA001_ReturnsGoldOrders()
    {
        var order = MakeOrder("pedra_dungeon");
        _orders.Setup(r => r.GetPagedByFilterAsync(null, null, "gold", null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { order } as IReadOnlyList<ShopOrder>, 1));

        var result = await CreateHandler().Handle(
            new GetAdminGoldOrdersQuery(null, null, null, null, null, 1, 20),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].ProductKey.Should().Be("pedra_dungeon");
        result.Items[0].Channel.Should().Be("gold");
    }

    // CA-002: status filtrado pelo repositório
    [Fact]
    public async Task Handle_CA002_StatusPassedToRepository()
    {
        _orders.Setup(r => r.GetPagedByFilterAsync(null, "granted", "gold", null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ShopOrder>(), 0));

        await CreateHandler().Handle(
            new GetAdminGoldOrdersQuery(null, "granted", null, null, null, 1, 20),
            CancellationToken.None);

        _orders.Verify(r => r.GetPagedByFilterAsync(null, "granted", "gold", null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-003: ProductKey filtra em memória (contains, case-insensitive)
    [Fact]
    public async Task Handle_CA003_ProductKeyFilter_InMemory()
    {
        var order1 = MakeOrder("pedra_dungeon");
        var order2 = MakeOrder("roupa_hunter");
        _orders.Setup(r => r.GetPagedByFilterAsync(null, null, "gold", null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { order1, order2 } as IReadOnlyList<ShopOrder>, 2));

        var result = await CreateHandler().Handle(
            new GetAdminGoldOrdersQuery(null, null, "pedra", null, null, 1, 20),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].ProductKey.Should().Be("pedra_dungeon");
    }

    // CA-004: paginação in-memory com ProductKey
    [Fact]
    public async Task Handle_CA004_InMemoryPagination_WithProductKey()
    {
        var orders = Enumerable.Range(1, 5).Select(i => MakeOrder($"item_{i}")).ToArray();
        _orders.Setup(r => r.GetPagedByFilterAsync(null, null, "gold", null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders as IReadOnlyList<ShopOrder>, orders.Length));

        var page1 = await CreateHandler().Handle(
            new GetAdminGoldOrdersQuery(null, null, "item", null, null, 1, 3),
            CancellationToken.None);
        var page2 = await CreateHandler().Handle(
            new GetAdminGoldOrdersQuery(null, null, "item", null, null, 2, 3),
            CancellationToken.None);

        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(2);
        page1.Total.Should().Be(5);
        page2.Total.Should().Be(5);
    }

    // CA-005: ExternalTransactionId ausente na resposta (RN-003)
    [Fact]
    public async Task Handle_CA005_ExternalTransactionId_NotExposedInResponse()
    {
        var order = MakeOrder("pedra_dungeon", externalId: "SENSITIVE_TRANSACTION_ID");
        _orders.Setup(r => r.GetPagedByFilterAsync(null, null, "gold", null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { order } as IReadOnlyList<ShopOrder>, 1));

        var result = await CreateHandler().Handle(
            new GetAdminGoldOrdersQuery(null, null, null, null, null, 1, 20),
            CancellationToken.None);

        // GoldOrderAdminResponse não tem campo ExternalTransactionId — verificação por reflexão
        var responseType = result.Items[0].GetType();
        responseType.GetProperty("ExternalTransactionId").Should().BeNull(
            "RN-003: dados de provider/pagamento não devem ser expostos");
    }
}
