using Nexora.Application.Orders.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>US-030 §7/§11 — prazo estimado (<c>promisedAt</c>/<c>estimatedMinutes</c>): o item mais demorado do pedido determina a estimativa (praças trabalham em paralelo).</summary>
public sealed class OrderPromiseCalculatorTests
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 7, 31, 20, 47, 0, TimeSpan.Zero);

    [Fact]
    public void Estimativa_Usa_O_Maior_Prep_Minutes_Entre_Os_Itens()
    {
        var estimate = OrderPromiseCalculator.Calculate(PlacedAt, new short[] { 8, 12, 5 });

        estimate.EstimatedMinutes.Should().Be(12);
        estimate.PromisedAt.Should().Be(PlacedAt.AddMinutes(12));
    }

    [Fact]
    public void Pedido_De_Item_Unico_Usa_O_Prep_Minutes_Dele()
    {
        var estimate = OrderPromiseCalculator.Calculate(PlacedAt, new short[] { 10 });

        estimate.EstimatedMinutes.Should().Be(10);
    }

    [Fact]
    public void Lista_Vazia_Nao_Lanca_E_Devolve_Estimativa_Zero()
    {
        var estimate = OrderPromiseCalculator.Calculate(PlacedAt, Array.Empty<short>());

        estimate.EstimatedMinutes.Should().Be(0);
        estimate.PromisedAt.Should().Be(PlacedAt);
    }
}
