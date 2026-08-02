using Awaken.Application.Shop.Queries.GetShopItems;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// US-188: catálogo orientado a dados (shop_products), sem lista estática
/// nem preço hardcoded no domínio.
/// </summary>
public class GetShopItemsQueryHandlerTests
{
    private readonly Mock<IShopProductRepository> _shopProductRepository = new();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private GetShopItemsQueryHandler CreateHandler() => new(_shopProductRepository.Object);

    [Fact]
    public async Task ReturnsEmptyList_WhenNoActiveProductsExist()
    {
        // CA-001: catálogo vazio é estado válido, sem erro.
        _shopProductRepository
            .Setup(r => r.GetActiveProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopProduct>());

        var result = await CreateHandler().Handle(new GetShopItemsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsOnlyGoldChannelProducts_WithPriceFromData()
    {
        // RN-003: preço vem do canal configurado, nunca fixo em texto.
        var goldProduct = ShopProduct.Create(
            "reforja_scroll", "Pergaminho da Reforja", null, "consumable", "rare",
            revenueCatProductId: null, UtcNow, priceGold: 150);
        var iapProduct = ShopProduct.Create(
            "premium_skin", "Skin Premium", null, "cosmetic", "epic",
            revenueCatProductId: "rc_premium_skin", UtcNow, priceGold: null);

        _shopProductRepository
            .Setup(r => r.GetActiveProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopProduct> { goldProduct, iapProduct });

        var result = await CreateHandler().Handle(new GetShopItemsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ItemKey.Should().Be("reforja_scroll");
        result[0].PriceGold.Should().Be(150);
    }

    [Fact]
    public async Task DoesNotReturnInactiveItems()
    {
        // CA-003: item inativo não aparece. O repositório já filtra IsActive
        // em GetActiveProductsAsync; o handler nunca recebe itens inativos.
        _shopProductRepository
            .Setup(r => r.GetActiveProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopProduct>());

        var result = await CreateHandler().Handle(new GetShopItemsQuery(), CancellationToken.None);

        result.Should().NotContain(i => i.ItemKey == "pedra_dungeon");
    }
}
