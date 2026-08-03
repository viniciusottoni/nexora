using Nexora.Application.Orders.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-032 (Carimbos de tempo T0 a T5) §12 (Estratégia de teste): "Unitário — Cálculo dos sete
/// intervalos, incluindo itens que não passam pelo gargalo" + "Propriedade — para qualquer ciclo
/// válido, nenhuma duração calculada é negativa". <see cref="OrderItemDurationCalculator"/> é a
/// extração pura (sem <c>IApplicationDbContext</c>) que sustenta <c>GetOrderItemTimelineQueryHandler</c>.
/// </summary>
public sealed class OrderItemDurationCalculatorTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Cenário Gherkin "Registro completo do ciclo" (US-032 §4) — item que passou pelo gargalo, todos os sete intervalos calculáveis.</summary>
    [Fact]
    public void Calculate_Item_Que_Passa_Pelo_Gargalo_Devolve_Os_Sete_Intervalos()
    {
        var placedAt = Base;
        var firedAt = Base.AddSeconds(214); // MET-001 fila
        var ovenInAt = firedAt.AddSeconds(96); // MET-002 montagem
        var ovenOutAt = ovenInAt.AddSeconds(420); // MET-003 cocção
        var readyAt = ovenOutAt.AddSeconds(30); // MET-004 finalização
        var servedAt = readyAt.AddSeconds(88); // MET-005 expedição

        var durations = OrderItemDurationCalculator.Calculate(placedAt, firedAt, ovenInAt, ovenOutAt, readyAt, servedAt);

        durations.QueueSeconds.Should().Be(214);
        durations.AssemblySeconds.Should().Be(96);
        durations.CookSeconds.Should().Be(420);
        durations.FinishSeconds.Should().Be(30);
        durations.ServeSeconds.Should().Be(88);
        durations.PrepSeconds.Should().Be(96 + 420 + 30, "MET-007 é T4-T1: montagem + cocção + finalização");
        durations.TotalSeconds.Should().Be(214 + 96 + 420 + 30 + 88, "MET-006 é T5-T0: soma de todos os intervalos");
    }

    /// <summary>Cenário Gherkin "Item que não passa pelo gargalo" (US-032 §4): refrigerante na praça Bebidas — ovenInAt/ovenOutAt nulos, MET-007 calculado direto T4-T1.</summary>
    [Fact]
    public void Calculate_Item_Que_Nao_Passa_Pelo_Gargalo_Ignora_Cozimento_Mas_Calcula_Producao_E_Total()
    {
        var placedAt = Base;
        var firedAt = Base.AddSeconds(60);
        var readyAt = firedAt.AddSeconds(45); // direto T4-T1, sem forno
        var servedAt = readyAt.AddSeconds(20);

        var durations = OrderItemDurationCalculator.Calculate(placedAt, firedAt, ovenInAt: null, ovenOutAt: null, readyAt, servedAt);

        durations.QueueSeconds.Should().Be(60);
        durations.AssemblySeconds.Should().BeNull("nunca entrou no forno — não existe T2");
        durations.CookSeconds.Should().BeNull("nunca entrou no forno — não existe T2/T3");
        durations.FinishSeconds.Should().BeNull("nunca saiu do forno — não existe T3");
        durations.ServeSeconds.Should().Be(20);
        durations.PrepSeconds.Should().Be(45, "T4-T1 direto, sem o gargalo no meio");
        durations.TotalSeconds.Should().Be(60 + 45 + 20);
    }

    /// <summary>Item ainda na fila — nenhum carimbo além de T0, todos os intervalos nulos.</summary>
    [Fact]
    public void Calculate_Item_Ainda_Na_Fila_Nao_Calcula_Nenhum_Intervalo()
    {
        var durations = OrderItemDurationCalculator.Calculate(Base, null, null, null, null, null);

        durations.Should().Be(new OrderItemDurationCalculator.Durations(null, null, null, null, null, null, null));
    }

    /// <summary>Item disparado mas ainda em produção (sem readyAt/servedAt) — só a fila é calculável.</summary>
    [Fact]
    public void Calculate_Item_Disparado_Mas_Ainda_Em_Producao_So_Calcula_A_Fila()
    {
        var firedAt = Base.AddSeconds(30);

        var durations = OrderItemDurationCalculator.Calculate(Base, firedAt, null, null, null, null);

        durations.QueueSeconds.Should().Be(30);
        durations.AssemblySeconds.Should().BeNull();
        durations.PrepSeconds.Should().BeNull("ainda não ficou pronto — T4 não existe");
        durations.TotalSeconds.Should().BeNull("ainda não foi servido — T5 não existe");
    }

    /// <summary>
    /// Teste de propriedade (US-032 §12) — FsCheck não está disponível neste repositório
    /// (confirmado em <c>Directory.Packages.props</c>), então esta é a alternativa combinatória
    /// exaustiva descrita no fallback da história: toda combinação de "o item avançou até aqui e
    /// parou" (inclui ou não passar pelo gargalo) gera uma sequência estritamente crescente de
    /// carimbos — nenhuma duração calculada pode ser negativa.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidStampSequences))]
    public void Calculate_Para_Qualquer_Sequencia_Valida_De_Carimbos_Nenhuma_Duracao_E_Negativa(
        DateTimeOffset placedAt,
        DateTimeOffset? firedAt,
        DateTimeOffset? ovenInAt,
        DateTimeOffset? ovenOutAt,
        DateTimeOffset? readyAt,
        DateTimeOffset? servedAt)
    {
        var durations = OrderItemDurationCalculator.Calculate(placedAt, firedAt, ovenInAt, ovenOutAt, readyAt, servedAt);

        durations.QueueSeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
        durations.AssemblySeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
        durations.CookSeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
        durations.FinishSeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
        durations.ServeSeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
        durations.PrepSeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
        durations.TotalSeconds.GetValueOrDefault(0).Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Gera toda combinação de "até onde o ciclo avançou" (7 pontos de parada: só T0, T0-T1, ...,
    /// T0-T5) cruzada com "passou ou não pelo gargalo" — cada carimbo presente é sempre
    /// estritamente posterior ao anterior (mesma garantia que <c>ck_item_sequence</c> impõe no
    /// banco), com deltas variados (1s a 600s) para não mascarar erro de sinal por coincidência.
    /// </summary>
    public static IEnumerable<object?[]> ValidStampSequences()
    {
        var placedAt = Base;

        // Ciclo completo passando pelo gargalo (pizza).
        var firedAt = placedAt.AddSeconds(37);
        var ovenInAt = firedAt.AddSeconds(58);
        var ovenOutAt = ovenInAt.AddSeconds(301);
        var readyAt = ovenOutAt.AddSeconds(12);
        var servedAt = readyAt.AddSeconds(145);

        yield return new object?[] { placedAt, null, null, null, null, null };
        yield return new object?[] { placedAt, firedAt, null, null, null, null };
        yield return new object?[] { placedAt, firedAt, ovenInAt, null, null, null };
        yield return new object?[] { placedAt, firedAt, ovenInAt, ovenOutAt, null, null };
        yield return new object?[] { placedAt, firedAt, ovenInAt, ovenOutAt, readyAt, null };
        yield return new object?[] { placedAt, firedAt, ovenInAt, ovenOutAt, readyAt, servedAt };

        // Ciclo completo SEM passar pelo gargalo (bebida) — mesmos deltas de fila/produção/expedição.
        var directReadyAt = firedAt.AddSeconds(64);
        var directServedAt = directReadyAt.AddSeconds(21);

        yield return new object?[] { placedAt, firedAt, null, null, directReadyAt, null };
        yield return new object?[] { placedAt, firedAt, null, null, directReadyAt, directServedAt };

        // Deltas mínimos (1s) — garante que arredondamento não produz -1 por sub-segundo.
        var tightFiredAt = placedAt.AddSeconds(1);
        var tightReadyAt = tightFiredAt.AddSeconds(1);
        var tightServedAt = tightReadyAt.AddSeconds(1);
        yield return new object?[] { placedAt, tightFiredAt, null, null, tightReadyAt, tightServedAt };

        // Carimbos simultâneos (delta zero) — válido pelo ck_item_sequence (">="), duração deve ser zero, nunca negativa.
        yield return new object?[] { placedAt, placedAt, placedAt, placedAt, placedAt, placedAt };
    }
}
