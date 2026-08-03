using Nexora.Application.Tables.Billing;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-027 (Dividir a conta) — testes da função pura de cálculo. US-027 §12 exige que a invariante
/// "para qualquer total e qualquer N, a soma das partes é igual ao total" seja um TESTE DE
/// PROPRIEDADE, não só exemplos. Este projeto não referencia nenhuma lib de property-based testing
/// (FsCheck não consta em <c>Directory.Packages.props</c> — conferido antes de escrever este
/// arquivo) — em vez de adicionar uma dependência nova só para uma invariante, os testes
/// <see cref="Invariante_Soma_Das_Partes_Igual_Ao_Total_Por_Pessoa"/> e
/// <see cref="Invariante_Soma_Das_Partes_Igual_Ao_Total_Por_Item"/> fazem uma bateria determinística
/// de centenas de combinações (totais "primos"/com muitas casas decimais incluídos) dentro de um
/// único <c>[Fact]</c> — mesmo espírito de um teste de propriedade (várias entradas geradas,
/// verificação de uma invariante universal), sem a dependência extra. Julgamento documentado aqui
/// para quem revisar depois perguntar "cadê o FsCheck".
/// </summary>
public sealed class BillSplitCalculatorTests
{
    // ---- Exemplos do Gherkin (US-027 §4) ----

    [Fact]
    public void Divisao_Por_Pessoa_Com_Residuo_Gera_100_Dividido_Por_3()
    {
        var result = BillSplitCalculator.CalculateByPerson(subtotal: 100m, serviceFeePercent: 0m, people: 3);

        result.Parts.Select(p => p.Amount).Should().BeEquivalentTo(new[] { 33.34m, 33.33m, 33.33m }, o => o.WithStrictOrdering());
        result.Parts.Sum(p => p.Amount).Should().Be(100m);
        result.Total.Should().Be(100m);
    }

    [Fact]
    public void Taxa_De_Servico_Proporcional_100_Mais_10_Por_Cento_Entre_4()
    {
        var result = BillSplitCalculator.CalculateByPerson(subtotal: 100m, serviceFeePercent: 10m, people: 4);

        result.Parts.Should().OnlyContain(p => p.Amount == 27.5m);
        result.Parts.Should().OnlyContain(p => p.ServiceFeeAmount == 2.5m);
        result.ServiceFeeNominal.Should().Be(10m);
        result.Total.Should().Be(110m);
        result.Parts.Sum(p => p.Amount).Should().Be(result.Total);
    }

    [Fact]
    public void Retirada_Da_Taxa_Por_Uma_Das_Partes_So_Recalcula_A_Parte_Dela()
    {
        var comTodos = BillSplitCalculator.CalculateByPerson(subtotal: 100m, serviceFeePercent: 10m, people: 4);
        var comRetirada = BillSplitCalculator.CalculateByPerson(subtotal: 100m, serviceFeePercent: 10m, people: 4, new HashSet<int> { 2 });

        // Só a parte da pessoa 2 muda — as outras continuam idênticas ao cenário sem retirada.
        comRetirada.Parts.Single(p => p.Person == 1).Should().BeEquivalentTo(comTodos.Parts.Single(p => p.Person == 1));
        comRetirada.Parts.Single(p => p.Person == 3).Should().BeEquivalentTo(comTodos.Parts.Single(p => p.Person == 3));
        comRetirada.Parts.Single(p => p.Person == 4).Should().BeEquivalentTo(comTodos.Parts.Single(p => p.Person == 4));

        var pessoaDois = comRetirada.Parts.Single(p => p.Person == 2);
        pessoaDois.ServiceFeeWaived.Should().BeTrue();
        pessoaDois.ServiceFeeAmount.Should().Be(0m);
        pessoaDois.Amount.Should().Be(25m, "sem a taxa, ela paga só o subtotal (100/4)");

        // Total efetivo cai exatamente pela taxa retirada — a casa recebe menos, ninguém mais paga a diferença.
        comRetirada.Total.Should().Be(comTodos.Total - comTodos.Parts.Single(p => p.Person == 2).ServiceFeeAmount);
        comRetirada.Parts.Sum(p => p.Amount).Should().Be(comRetirada.Total);
    }

    [Fact]
    public void Divisao_Por_Item_Cada_Parte_Contem_Apenas_Os_Itens_Atribuidos()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        var itemC = Guid.NewGuid();

        var items = new[]
        {
            new BillSplitItem(itemA, "Pizza", 40m, Pending: false, AssignedPerson: 1),
            new BillSplitItem(itemB, "Refrigerante", 8m, Pending: false, AssignedPerson: 1),
            new BillSplitItem(itemC, "Sobremesa", 20m, Pending: false, AssignedPerson: 2),
        };

        var result = BillSplitCalculator.CalculateByItem(items, serviceFeePercent: 0m);

        result.UnassignedItemIds.Should().BeEmpty();
        result.Parts.Single(p => p.Person == 1).Amount.Should().Be(48m);
        result.Parts.Single(p => p.Person == 2).Amount.Should().Be(20m);
        result.Parts.Sum(p => p.Amount).Should().Be(result.Total);
    }

    [Fact]
    public void Divisao_Por_Item_Reporta_Itens_Nao_Atribuidos()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();

        var items = new[]
        {
            new BillSplitItem(itemA, "Pizza", 40m, Pending: false, AssignedPerson: 1),
            new BillSplitItem(itemB, "Refrigerante", 8m, Pending: false, AssignedPerson: null),
        };

        var result = BillSplitCalculator.CalculateByItem(items, serviceFeePercent: 10m);

        result.UnassignedItemIds.Should().ContainSingle().Which.Should().Be(itemB);
        // Item órfão não entra no subtotal/total calculado (a Application recusa fechar antes disso).
        result.Subtotal.Should().Be(40m);
    }

    [Fact]
    public void Divisao_Por_Valor_Calcula_Restante_Em_Aberto()
    {
        var result = BillSplitCalculator.CalculateByAmount(total: 180m, alreadyPaid: 0m, amountNow: 50m);

        result.Remaining.Should().Be(130m);
        (result.AlreadyPaid + result.AmountNow + result.Remaining).Should().Be(result.Total);
    }

    [Fact]
    public void Divisao_Por_Valor_Recusa_Valor_Maior_Que_O_Saldo()
    {
        var act = () => BillSplitCalculator.CalculateByAmount(total: 100m, alreadyPaid: 60m, amountNow: 50m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Divisao_Por_Valor_Recusa_Valor_Zero_Ou_Negativo()
    {
        var actZero = () => BillSplitCalculator.CalculateByAmount(total: 100m, alreadyPaid: 0m, amountNow: 0m);
        var actNegativo = () => BillSplitCalculator.CalculateByAmount(total: 100m, alreadyPaid: 0m, amountNow: -1m);

        actZero.Should().Throw<ArgumentOutOfRangeException>();
        actNegativo.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---- Valores "primos" e casas decimais difíceis (US-027 §12/§15) ----

    [Theory]
    [InlineData(7, 3)]
    [InlineData(11, 7)]
    [InlineData(13, 13)]
    [InlineData(17, 5)]
    [InlineData(101, 7)]
    [InlineData(0.01, 3)]
    [InlineData(0.02, 7)]
    [InlineData(9999.99, 11)]
    [InlineData(1, 3)]
    [InlineData(1000000.01, 17)]
    public void Soma_Das_Partes_Bate_Com_O_Total_Para_Valores_Dificeis(decimal subtotal, int people)
    {
        var result = BillSplitCalculator.CalculateByPerson(subtotal, serviceFeePercent: 10m, people);

        result.Parts.Sum(p => p.Amount).Should().Be(result.Total);
        result.Total.Should().Be(subtotal + result.Parts.Sum(p => p.ServiceFeeAmount));
    }

    [Fact]
    public void DistributeEqually_Sempre_Soma_Exatamente_Ao_Total()
    {
        for (var parts = 1; parts <= 13; parts++)
        {
            foreach (var total in new[] { 0m, 0.01m, 0.02m, 1m, 7m, 13m, 17m, 100m, 99.99m, 1234567.89m })
            {
                var shares = BillSplitCalculator.DistributeEqually(total, parts);
                shares.Sum().Should().Be(total, $"total={total}, parts={parts}");
                shares.Should().HaveCount(parts);
            }
        }
    }

    /// <summary>
    /// TESTE DE PROPRIEDADE (US-027 §12/§15): "para qualquer total e qualquer N, a soma das partes é
    /// igual ao total". Gera uma bateria ampla e determinística de totais (incluindo números primos
    /// de centavos e valores com muitas casas decimais truncadas a 2, conforme a especificação
    /// monetária ADR-017) cruzados com N de 1 a 47 (primo), sem taxa de serviço e com taxa de
    /// serviço variada — nenhuma combinação pode quebrar a invariante.
    /// </summary>
    [Fact]
    public void Invariante_Soma_Das_Partes_Igual_Ao_Total_Por_Pessoa()
    {
        var primeCents = new[] { 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 997, 7919 };
        var feePercents = new[] { 0m, 5m, 7.5m, 10m, 12.35m, 33.33m };
        var peopleCounts = Enumerable.Range(1, 47).ToArray();

        var random = new Random(20260802); // seed fixo — reprodutível entre execuções
        var casosVerificados = 0;

        foreach (var cents in primeCents)
        {
            var subtotal = cents / 100m + random.Next(0, 100000) / 100m;

            foreach (var people in peopleCounts)
            {
                foreach (var feePercent in feePercents)
                {
                    var result = BillSplitCalculator.CalculateByPerson(subtotal, feePercent, people);

                    result.Parts.Should().HaveCount(people);
                    result.Parts.Sum(p => p.Amount).Should().Be(
                        result.Total, $"subtotal={subtotal}, people={people}, fee%={feePercent}");
                    result.Total.Should().Be(subtotal + result.ServiceFeeNominal);

                    casosVerificados++;
                }
            }
        }

        casosVerificados.Should().Be(primeCents.Length * peopleCounts.Length * feePercents.Length);
        casosVerificados.Should().BeGreaterThan(7000, "a bateria precisa cobrir centenas/milhares de combinações, não só alguns exemplos");
    }

    /// <summary>Mesma invariante do teste acima, agora no modo BY_ITEM — pesos de subtotal desiguais e "difíceis" por pessoa.</summary>
    [Fact]
    public void Invariante_Soma_Das_Partes_Igual_Ao_Total_Por_Item()
    {
        var random = new Random(729);
        var casosVerificados = 0;

        for (var trial = 0; trial < 500; trial++)
        {
            var peopleCount = random.Next(1, 20);
            var itemsPerPerson = random.Next(1, 8);
            var feePercent = random.Next(0, 3000) / 100m;

            var items = new List<BillSplitItem>();
            for (var person = 1; person <= peopleCount; person++)
            {
                for (var i = 0; i < itemsPerPerson; i++)
                {
                    var cents = random.Next(1, 999999);
                    items.Add(new BillSplitItem(Guid.NewGuid(), "Item", cents / 100m, Pending: false, AssignedPerson: person));
                }
            }

            var result = BillSplitCalculator.CalculateByItem(items, feePercent);

            result.UnassignedItemIds.Should().BeEmpty();
            result.Parts.Sum(p => p.Amount).Should().Be(result.Total, $"trial={trial}, people={peopleCount}, itemsPerPerson={itemsPerPerson}, fee%={feePercent}");

            casosVerificados++;
        }

        casosVerificados.Should().Be(500);
    }

    [Fact]
    public void DistributeProportional_Com_Peso_Zero_Nao_Quebra_A_Soma()
    {
        var shares = BillSplitCalculator.DistributeProportional(10m, new[] { 0m, 0m, 0m });
        shares.Sum().Should().Be(10m);
    }
}
