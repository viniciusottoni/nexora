using System.Reflection;
using Awaken.Domain.Entities.Shop;
using FluentAssertions;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// US-188 / CA-002: o domínio não deve conter lista estática de itens de
/// loja nem preço Gold fixo em código. O catálogo (ShopProduct.PriceGold)
/// é dado, configurado em shop_products.
/// </summary>
public class NoStaticShopCatalogTests
{
    [Fact]
    public void DomainAssembly_DoesNotContainStaticShopCatalogType()
    {
        var domainAssembly = typeof(ShopProduct).Assembly;

        var staticCatalogTypes = domainAssembly
            .GetTypes()
            .Where(t => t.Name is "ShopCatalog" or "ShopCatalogItem")
            .ToList();

        staticCatalogTypes.Should().BeEmpty(
            "the static ShopCatalog/ShopCatalogItem types must be removed; the catalog is data-driven (shop_products)");
    }

    [Fact]
    public void ShopProduct_DoesNotExposeHardcodedPriceConstants()
    {
        var fields = typeof(ShopProduct)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(f => f.IsLiteral && f.FieldType == typeof(int))
            .ToList();

        fields.Should().BeEmpty("ShopProduct must not declare fixed/hardcoded gold price constants");
    }
}
