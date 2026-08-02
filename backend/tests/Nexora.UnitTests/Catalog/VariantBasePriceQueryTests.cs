using FluentAssertions;
using Nexora.Application.Catalog.Variants;
using Nexora.Domain.Catalog;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class VariantBasePriceQueryTests
{
    [Fact]
    public void Deve_Selecionar_Apenas_Preco_Vigente_Do_Canal_Balcao()
    {
        var tenantId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var prices = new[]
        {
            Price.Create(tenantId, variantId, Channel.Delivery, 59m),
            Price.Create(tenantId, variantId, Channel.DineIn, 45m),
            Price.Create(tenantId, Guid.NewGuid(), Channel.DineIn, 10m)
        }.AsQueryable();

        var selected = prices.CurrentDineInFor(variantId).Single();

        selected.Channel.Should().Be(Channel.DineIn);
        selected.Amount.Should().Be(45m);
    }
}
