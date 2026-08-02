using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Domain.Catalog;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-014 (Preço por canal de venda) §3.1/§12 — regra de herança de preço por canal, pura, sem
/// I/O: quando um canal não tem preço vigente próprio, o preço vigente de <see cref="Channel.DineIn"/>
/// é usado como base (cenário Gherkin "Herança do preço base"). Cobre só
/// <see cref="ChannelPriceResolver"/>, que não toca banco — o fluxo completo via
/// <c>ListVariantPricesByChannelQueryHandler</c> é coberto em
/// <c>Nexora.IntegrationTests.PricingIntegrationTests</c>.
/// </summary>
public sealed class PriceChannelInheritanceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();

    [Fact]
    public void Canal_Com_Preco_Proprio_Nao_Herda_Do_Base()
    {
        var dineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);
        var delivery = Price.Create(TenantId, VariantId, Channel.Delivery, 52.00m);

        var resolved = ChannelPriceResolver.Resolve(Channel.Delivery, new[] { dineIn, delivery });

        resolved.Amount.Should().Be(52.00m);
        resolved.IsInherited.Should().BeFalse();
        resolved.SourcePriceId.Should().Be(delivery.Id);
    }

    [Fact]
    public void Canal_Sem_Preco_Proprio_Herda_Do_DineIn()
    {
        var dineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);

        var resolved = ChannelPriceResolver.Resolve(Channel.Takeout, new[] { dineIn });

        resolved.Amount.Should().Be(45.00m, "sem preço próprio de balcão, o preço base do salão é aplicado (US-014, cenário 'Herança do preço base')");
        resolved.IsInherited.Should().BeTrue();
        resolved.SourcePriceId.Should().Be(dineIn.Id);
        resolved.ValidFrom.Should().Be(dineIn.ValidFrom);
    }

    [Fact]
    public void Marketplace_Sem_Preco_Proprio_Tambem_Herda_Do_DineIn()
    {
        var dineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);

        var resolved = ChannelPriceResolver.Resolve(Channel.Marketplace, new[] { dineIn });

        resolved.IsInherited.Should().BeTrue();
        resolved.Amount.Should().Be(45.00m);
    }

    [Fact]
    public void DineIn_Sem_Preco_Proprio_Nao_Tem_De_Onde_Herdar()
    {
        var delivery = Price.Create(TenantId, VariantId, Channel.Delivery, 52.00m);

        var resolved = ChannelPriceResolver.Resolve(Channel.DineIn, new[] { delivery });

        resolved.Amount.Should().BeNull("DineIn é o próprio canal-base; sem preço nele não há de onde herdar");
        resolved.IsInherited.Should().BeFalse();
    }

    [Fact]
    public void Canal_Sem_Preco_Proprio_E_Sem_Base_Retorna_Nulo()
    {
        var resolved = ChannelPriceResolver.Resolve(Channel.Delivery, Array.Empty<Price>());

        resolved.Amount.Should().BeNull();
        resolved.IsInherited.Should().BeFalse();
    }

    [Fact]
    public void Preco_Fechado_Nao_E_Considerado_Vigente()
    {
        // ChannelPriceResolver recebe só preços já filtrados a ValidTo == null pelo handler — um
        // preço fechado passado por engano não deve ser tratado como vigente do canal.
        var closedDineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 40.00m);
        closedDineIn.Close(DateTimeOffset.UtcNow);
        var openDelivery = Price.Create(TenantId, VariantId, Channel.Delivery, 52.00m);

        var resolved = ChannelPriceResolver.Resolve(Channel.DineIn, new[] { openDelivery });

        resolved.Amount.Should().BeNull("o preço de DineIn passado estava fechado e não deveria ter sido incluído na coleção de vigentes");
    }

    [Fact]
    public void ResolveAll_Traz_Os_Quatro_Canais_Na_Ordem_De_Exibicao()
    {
        var dineIn = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);
        var delivery = Price.Create(TenantId, VariantId, Channel.Delivery, 52.00m);

        var rows = ChannelPriceResolver.ResolveAll(new[] { dineIn, delivery });

        rows.Should().HaveCount(4);
        rows.Select(r => r.Channel).Should().ContainInOrder(Channel.DineIn, Channel.Delivery, Channel.Takeout, Channel.Marketplace);
        rows.Single(r => r.Channel == Channel.DineIn).IsInherited.Should().BeFalse();
        rows.Single(r => r.Channel == Channel.Delivery).IsInherited.Should().BeFalse();
        rows.Single(r => r.Channel == Channel.Takeout).IsInherited.Should().BeTrue();
        rows.Single(r => r.Channel == Channel.Takeout).Amount.Should().Be(45.00m);
        rows.Single(r => r.Channel == Channel.Marketplace).IsInherited.Should().BeTrue();
    }
}
