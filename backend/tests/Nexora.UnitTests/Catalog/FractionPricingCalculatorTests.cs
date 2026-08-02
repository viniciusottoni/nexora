using Nexora.Application.Catalog.FractionPricing;
using Nexora.Shared.Errors;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-013 (Pizza meio a meio com frações) §12 — as três regras de precificação com 2, 3 e 4
/// frações, rejeição por <c>size_code</c>/<c>fraction_group</c> divergente, soma de pesos e
/// arredondamento em centavos. Cobre só <see cref="FractionPricingCalculator"/> (função pura, sem
/// I/O) — o fluxo completo via <c>PreviewFractionPricingQueryHandler</c> (carga de
/// variante/produto/preço do banco) é coberto em
/// <c>Nexora.IntegrationTests.FractionPricingIntegrationTests</c>.
/// </summary>
public sealed class FractionPricingCalculatorTests
{
    private static FractionPricingLine Line(decimal weight, decimal unitPrice, string sizeCode = "G", string? fractionGroup = "PIZZA") =>
        new(Guid.NewGuid(), weight, unitPrice, sizeCode, fractionGroup);

    // ---------------------------------------------------------------------
    // Cenário Gherkin "Precificação por maior valor" (US-013 §4).
    // ---------------------------------------------------------------------
    [Fact]
    public void Highest_Com_Duas_Fracoes_Retorna_O_Maior_Preco()
    {
        var fractions = new[] { Line(0.5m, 45.00m), Line(0.5m, 52.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitPrice.Should().Be(52.00m);
        result.Value.Rule.Should().Be(FractionPriceRule.Highest);
    }

    [Fact]
    public void Highest_Com_Tres_Fracoes_Retorna_O_Maior_Entre_Os_Tres()
    {
        // Pesos 0.3333/0.3333/0.3334 (não 1m/3 repetido): decimal não representa 1/3 exatamente,
        // e 0.3333333...*3 fica em 0.999...9, nunca fechando em 1,0 — a conciliação de ADR-017
        // ("a sobra vai para a primeira parcela"... aqui, por simplicidade, para a última) exige
        // literais que somem exatamente 1,0000.
        var fractions = new[]
        {
            Line(0.3333m, 40.00m),
            Line(0.3333m, 45.00m),
            Line(0.3334m, 38.00m),
        };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitPrice.Should().Be(45.00m);
    }

    [Fact]
    public void Highest_Com_Quatro_Fracoes_Retorna_O_Maior_Entre_Os_Quatro()
    {
        var fractions = new[] { Line(0.25m, 40m), Line(0.25m, 42m), Line(0.25m, 39m), Line(0.25m, 55m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.Value!.UnitPrice.Should().Be(55m);
    }

    // ---------------------------------------------------------------------
    // Cenário Gherkin "Precificação por média" (US-013 §4).
    // ---------------------------------------------------------------------
    [Fact]
    public void Average_Com_Duas_Fracoes_Calcula_A_Media_Aritmetica()
    {
        var fractions = new[] { Line(0.5m, 45.00m), Line(0.5m, 52.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Average);

        result.Value!.UnitPrice.Should().Be(48.50m);
    }

    [Fact]
    public void Average_Ignora_O_Peso_Mesmo_Quando_Os_Pesos_Sao_Diferentes()
    {
        // Pesos assimetricos (0.25/0.75) não alteram a média simples — só PROPORTIONAL usa peso.
        var fractions = new[] { Line(0.25m, 40.00m), Line(0.75m, 60.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Average);

        result.Value!.UnitPrice.Should().Be(50.00m);
    }

    [Fact]
    public void Average_Com_Tres_Sabores_De_Pesos_Iguais()
    {
        var fractions = new[] { Line(0.3333m, 40.00m), Line(0.3333m, 46.00m), Line(0.3334m, 43.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Average);

        result.Value!.UnitPrice.Should().Be(43.00m, "AVERAGE ignora o peso — (40 + 46 + 43) / 3 = 43.00 exato");
    }

    [Fact]
    public void Average_Com_Valores_Impares_Arredonda_Half_Up_Sem_Perda()
    {
        // (45.01 + 45.02) / 2 = 45.015 -> half-up -> 45.02 (ADR-017, política escolhida na
        // implementação: MidpointRounding.AwayFromZero, mesmo padrão de BulkPriceAdjustmentCalculator).
        var fractions = new[] { Line(0.5m, 45.01m), Line(0.5m, 45.02m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Average);

        result.Value!.UnitPrice.Should().Be(45.02m);
    }

    [Fact]
    public void Average_Com_Tres_Valores_Que_Nao_Dividem_Exato_Arredonda_Half_Up()
    {
        // (10.00 + 10.00 + 10.01) / 3 = 10.003333... -> half-up em 2 casas -> 10.00.
        var fractions = new[] { Line(0.3333m, 10.00m), Line(0.3333m, 10.00m), Line(0.3334m, 10.01m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Average);

        result.Value!.UnitPrice.Should().Be(10.00m);
    }

    // ---------------------------------------------------------------------
    // Cenário Gherkin "Precificação proporcional" (US-013 §4).
    // ---------------------------------------------------------------------
    [Fact]
    public void Proportional_Com_Pesos_Iguais_Coincide_Com_A_Media()
    {
        var fractions = new[] { Line(0.5m, 45.00m), Line(0.5m, 52.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Proportional);

        result.Value!.UnitPrice.Should().Be(48.50m, "cenário Gherkin: pesos iguais -> mesmo resultado da regra AVERAGE");
    }

    [Fact]
    public void Proportional_Com_Tres_Sabores_De_Pesos_Iguais_E_A_Soma_Ponderada()
    {
        // Cenário Gherkin "com três sabores de pesos iguais, deve ser a soma ponderada dos três"
        // (US-013 §4). Pesos 0.3333/0.3333/0.3334 (a divisão exata 1/3 não fecha em decimal —
        // ver nota em Highest_Com_Tres_Fracoes_Retorna_O_Maior_Entre_Os_Tres).
        var fractions = new[] { Line(0.3333m, 40.00m), Line(0.3333m, 46.00m), Line(0.3334m, 43.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Proportional);

        result.Value!.UnitPrice.Should().Be(43.00m, "0.3333*40 + 0.3333*46 + 0.3334*43 = 43.00 — pesos quase iguais entre 3 sabores");
    }

    [Fact]
    public void Proportional_Com_Pesos_Assimetricos_Pondera_Pelo_Peso_De_Cada_Fracao()
    {
        // 0.25 * 40.00 + 0.75 * 60.00 = 10.00 + 45.00 = 55.00 — diferente da média simples (50.00),
        // prova que PROPORTIONAL de fato usa o peso (diferente de AVERAGE).
        var fractions = new[] { Line(0.25m, 40.00m), Line(0.75m, 60.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Proportional);

        result.Value!.UnitPrice.Should().Be(55.00m);
    }

    [Fact]
    public void Proportional_Com_Quatro_Fracoes_De_Pesos_Iguais()
    {
        var fractions = new[] { Line(0.25m, 40m), Line(0.25m, 42m), Line(0.25m, 39m), Line(0.25m, 55m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Proportional);

        result.Value!.UnitPrice.Should().Be(44.00m, "(40+42+39+55)/4 = 44.00, pesos iguais");
    }

    // ---------------------------------------------------------------------
    // Cenário Gherkin "Tamanhos incompatíveis" (US-013 §4).
    // ---------------------------------------------------------------------
    [Fact]
    public void Tamanhos_Divergentes_Sao_Recusados_Com_Codigo_Especifico()
    {
        var fractions = new[] { Line(0.5m, 45.00m, sizeCode: "G"), Line(0.5m, 40.00m, sizeCode: "M") };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionSizeMismatch);
        result.Errors.Should().ContainKey("sizes");
        result.Errors!["sizes"].Should().Contain(new[] { "G", "M" });
    }

    // ---------------------------------------------------------------------
    // Cenário Gherkin "Grupos de fração distintos" (US-013 §4).
    // ---------------------------------------------------------------------
    [Fact]
    public void Grupos_De_Fracao_Divergentes_Sao_Recusados_Com_Codigo_Especifico()
    {
        var fractions = new[]
        {
            Line(0.5m, 45.00m, fractionGroup: "PIZZA"),
            Line(0.5m, 30.00m, fractionGroup: "HAMBURGUER"),
        };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionGroupMismatch);
        result.Errors.Should().ContainKey("groups");
    }

    [Fact]
    public void Grupo_De_Fracao_Ausente_E_Recusado()
    {
        var fractions = new[]
        {
            Line(0.5m, 45.00m, fractionGroup: null),
            Line(0.5m, 52.00m, fractionGroup: null),
        };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionGroupMismatch);
    }

    [Fact]
    public void Mesmo_Sabor_Repetido_E_Recusado()
    {
        var variantId = Guid.NewGuid();
        var fractions = new[]
        {
            new FractionPricingLine(variantId, 0.5m, 45.00m, "G", "PIZZA"),
            new FractionPricingLine(variantId, 0.5m, 45.00m, "G", "PIZZA"),
        };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionMinimumNotMet);
    }

    // ---------------------------------------------------------------------
    // Cenário Gherkin "Montagem de meio a meio" (US-013 §4) — soma de pesos.
    // ---------------------------------------------------------------------
    [Fact]
    public void Soma_De_Pesos_Que_Nao_Fecha_Em_Um_E_Recusada()
    {
        // 0.5 + 0.4 = 0.9 (não fecha em 1,0).
        var fractions = new[] { Line(0.5m, 45.00m), Line(0.4m, 52.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionWeightSumInvalid);
    }

    [Fact]
    public void Soma_De_Pesos_Maior_Que_Um_Tambem_E_Recusada()
    {
        var fractions = new[] { Line(0.6m, 45.00m), Line(0.6m, 52.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionWeightSumInvalid);
    }

    [Fact]
    public void Soma_De_Pesos_Com_Tres_Fracoes_Exatamente_Um_E_Aceita()
    {
        // 0.3333 + 0.3333 + 0.3334 = 1.0000 exato — conciliação na última parcela (ADR-017).
        var fractions = new[] { Line(0.3333m, 40m), Line(0.3333m, 46m), Line(0.3334m, 43m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Estrutural: mínimo de duas frações.
    // ---------------------------------------------------------------------
    [Fact]
    public void Uma_Unica_Fracao_E_Recusada_Por_Nao_Configurar_Meio_A_Meio()
    {
        var fractions = new[] { Line(1.0m, 45.00m) };

        var result = FractionPricingCalculator.Calculate(fractions, FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionMinimumNotMet);
    }

    [Fact]
    public void Lista_Vazia_E_Recusada()
    {
        var result = FractionPricingCalculator.Calculate(Array.Empty<FractionPricingLine>(), FractionPriceRule.Highest);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionMinimumNotMet);
    }
}
