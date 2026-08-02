using FluentAssertions;
using Nexora.Application.Catalog.Products.Queries.GetPublicMenu;
using Nexora.Domain.Catalog;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class PublicMenuPriceResolverTests
{
    [Fact]
    public void ResolveFromPrice_Usa_Preco_Do_Canal_Quando_Existe()
    {
        var variant = Guid.NewGuid();
        var prices = new[]
        {
            new PublicMenuCurrentPrice(variant, Channel.DineIn, 45m),
            new PublicMenuCurrentPrice(variant, Channel.Delivery, 52m),
        };

        PublicMenuPriceResolver.ResolveFromPrice(Channel.Delivery, prices).Should().Be(52m);
    }

    [Fact]
    public void ResolveFromPrice_Herda_DineIn_Por_Variante_Antes_De_Selecionar_O_Menor()
    {
        var inheritedVariant = Guid.NewGuid();
        var ownVariant = Guid.NewGuid();
        var prices = new[]
        {
            new PublicMenuCurrentPrice(inheritedVariant, Channel.DineIn, 35m),
            new PublicMenuCurrentPrice(ownVariant, Channel.DineIn, 40m),
            new PublicMenuCurrentPrice(ownVariant, Channel.Delivery, 48m),
        };

        PublicMenuPriceResolver.ResolveFromPrice(Channel.Delivery, prices).Should().Be(35m);
    }

    [Fact]
    public void ResolveFromPrice_Sem_Preco_Do_Canal_Nem_DineIn_Retorna_Nulo()
    {
        var prices = new[]
        {
            new PublicMenuCurrentPrice(Guid.NewGuid(), Channel.Takeout, 45m),
        };

        PublicMenuPriceResolver.ResolveFromPrice(Channel.Delivery, prices).Should().BeNull();
    }
}
