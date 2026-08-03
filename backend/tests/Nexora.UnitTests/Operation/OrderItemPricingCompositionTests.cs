using Nexora.Application.Catalog.FractionPricing;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-030 §12 ("Cálculo de preço com modificadores, frações e canal") — cobre a composição
/// completa que <c>CreateOrderCommandHandler</c>/<c>AddOrderItemCommandHandler</c> exercitam contra
/// o banco: preço vigente por CANAL (<see cref="ChannelPriceResolver"/>, US-014), preço de item com
/// FRAÇÃO nas três regras de RN-009 (<see cref="FractionPricingCalculator"/>, US-013) e MODIFICADOR
/// somado ao total do item (<see cref="OrderItem.AddModifier"/>). Tudo em funções puras/domínio,
/// sem <c>IApplicationDbContext</c> — a parte de I/O (carregar preço/variante do banco) já é
/// coberta pelos testes de integração de US-030.
/// </summary>
public sealed class OrderItemPricingCompositionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();

    [Fact]
    public void Pizza_Meio_A_Meio_Regra_Highest_Usa_O_Maior_Preco_Entre_As_Fracoes()
    {
        var fractions = new[]
        {
            new FractionPricingLine(Guid.NewGuid(), 0.5m, UnitPrice: 45.00m, SizeCode: "G", FractionGroup: "PIZZA"),
            new FractionPricingLine(Guid.NewGuid(), 0.5m, UnitPrice: 52.00m, SizeCode: "G", FractionGroup: "PIZZA"),
        };

        var calculation = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);
        calculation.IsSuccess.Should().BeTrue();
        calculation.Value!.UnitPrice.Should().Be(52.00m);

        var item = OrderItem.Create(TenantId, OrderId, VariantId, calculation.Value.UnitPrice);
        item.AddModifier(OrderItemModifier.Create(TenantId, item.Id, Guid.NewGuid(), "Borda catupiry", priceDelta: 8.00m));

        item.TotalPrice.Should().Be(60.00m, "52,00 (fração mais cara) + 8,00 (borda) — mesmo exemplo do contrato de API da US-030 §7");
    }

    [Fact]
    public void Pizza_Meio_A_Meio_Regra_Average_Usa_A_Media_Simples_Das_Fracoes()
    {
        var fractions = new[]
        {
            new FractionPricingLine(Guid.NewGuid(), 0.5m, UnitPrice: 40.00m, SizeCode: "G", FractionGroup: "PIZZA"),
            new FractionPricingLine(Guid.NewGuid(), 0.5m, UnitPrice: 50.00m, SizeCode: "G", FractionGroup: "PIZZA"),
        };

        var calculation = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Average);

        calculation.IsSuccess.Should().BeTrue();
        calculation.Value!.UnitPrice.Should().Be(45.00m);

        var item = OrderItem.Create(TenantId, OrderId, VariantId, calculation.Value.UnitPrice, quantity: 2);
        item.TotalPrice.Should().Be(90.00m);
    }

    [Fact]
    public void Pizza_Meio_A_Meio_Regra_Proportional_Pondera_Pelo_Peso_De_Cada_Fracao()
    {
        // 0,75 de mussarela (40) + 0,25 de calabresa (60) = 30 + 15 = 45
        var fractions = new[]
        {
            new FractionPricingLine(Guid.NewGuid(), 0.75m, UnitPrice: 40.00m, SizeCode: "G", FractionGroup: "PIZZA"),
            new FractionPricingLine(Guid.NewGuid(), 0.25m, UnitPrice: 60.00m, SizeCode: "G", FractionGroup: "PIZZA"),
        };

        var calculation = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Proportional);

        calculation.IsSuccess.Should().BeTrue();
        calculation.Value!.UnitPrice.Should().Be(45.00m);
    }

    /// <summary>Cenário Gherkin "Preço aplicado por canal" (US-030 §4): DINE_IN tem preço próprio, DELIVERY herda o base quando não tem preço próprio.</summary>
    [Fact]
    public void Item_Sem_Fracao_Resolve_Preco_Proprio_Do_Canal_Sem_Herdar_Quando_Tem_Preco_Proprio()
    {
        var dineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);
        var delivery = Price.Create(TenantId, VariantId, Channel.Delivery, 52.00m);

        var resolvedDineIn = ChannelPriceResolver.Resolve(Channel.DineIn, new[] { dineIn, delivery });
        var resolvedDelivery = ChannelPriceResolver.Resolve(Channel.Delivery, new[] { dineIn, delivery });

        resolvedDineIn.Amount.Should().Be(45.00m);
        resolvedDelivery.Amount.Should().Be(52.00m);

        var itemDineIn = OrderItem.Create(TenantId, OrderId, VariantId, resolvedDineIn.Amount!.Value);
        var itemDelivery = OrderItem.Create(TenantId, OrderId, VariantId, resolvedDelivery.Amount!.Value);

        itemDineIn.TotalPrice.Should().Be(45.00m);
        itemDelivery.TotalPrice.Should().Be(52.00m);
    }

    [Fact]
    public void Item_Sem_Preco_Proprio_No_Canal_Herda_O_Preco_De_DineIn()
    {
        var dineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);

        var resolvedTakeout = ChannelPriceResolver.Resolve(Channel.Takeout, new[] { dineIn });

        resolvedTakeout.Amount.Should().Be(45.00m);
        resolvedTakeout.IsInherited.Should().BeTrue();
    }
}
