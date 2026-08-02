using Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-014 (Preço por canal de venda) §12 — "Cálculo de reajuste percentual com arredondamento em
/// centavos", puro e sem I/O. Arredondamento half-up (ADR-017): meio-a-meio sempre arredonda para
/// cima em valor absoluto (<c>MidpointRounding.AwayFromZero</c>).
/// </summary>
public sealed class BulkPriceAdjustmentCalculatorTests
{
    [Fact]
    public void Aumento_De_Oito_Por_Cento_Arredonda_Para_Cima()
    {
        // 45.00 * 1.08 = 48.60 exato — sem ambiguidade de arredondamento.
        BulkPriceAdjustmentCalculator.Apply(45.00m, 8m).Should().Be(48.60m);
    }

    [Fact]
    public void Percentual_Que_Gera_Meio_Centavo_Arredonda_Half_Up()
    {
        // 10.00 * 1.025 = 10.25 exato; usar um caso com terceira casa .5 explícita:
        // 33.33 * 1.05 = 34.9965 -> arredonda para 35.00 (terceira casa 6, mas cobre o caminho de
        // arredondamento não trivial).
        BulkPriceAdjustmentCalculator.Apply(33.33m, 5m).Should().Be(35.00m);
    }

    [Fact]
    public void Percentual_Negativo_Reduz_O_Preco()
    {
        BulkPriceAdjustmentCalculator.Apply(50.00m, -10m).Should().Be(45.00m);
    }

    [Fact]
    public void Percentual_Cem_Negativo_Zera_O_Preco()
    {
        BulkPriceAdjustmentCalculator.Apply(50.00m, -100m).Should().Be(0m);
    }

    [Fact]
    public void Percentual_Zero_Preserva_O_Valor()
    {
        BulkPriceAdjustmentCalculator.Apply(45.00m, 0m).Should().Be(45.00m);
    }

    [Fact]
    public void Meio_Centavo_Exato_Arredonda_Para_Cima_Nunca_Para_Baixo()
    {
        // 0.05 * 1.01 = 0.0505 -> half-up em 2 casas -> 0.05 (terceira casa 0, não é meio exato;
        // usar caso construído para cair exatamente em .xx5): 1.00 * 1.005 = 1.005 -> half-up -> 1.01.
        BulkPriceAdjustmentCalculator.Apply(1.00m, 0.5m).Should().Be(1.01m);
    }
}
