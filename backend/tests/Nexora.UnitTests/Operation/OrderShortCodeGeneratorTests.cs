using Nexora.Application.Orders.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>US-030 §8/ADR-016 — equivalente puro de <c>next_short_code()</c> (Docs/Domain/11-Views-e-Funcoes.md).</summary>
public sealed class OrderShortCodeGeneratorTests
{
    [Fact]
    public void NextSequence_Sem_Nenhum_Codigo_Do_Dia_Comeca_Em_1()
    {
        var sequence = OrderShortCodeGenerator.NextSequence(Array.Empty<string>(), 'A');

        sequence.Should().Be(1);
    }

    [Fact]
    public void NextSequence_Usa_O_Maior_Numero_Do_Prefixo_Mais_Um()
    {
        var existing = new[] { "A1", "A2", "A15", "A3" };

        var sequence = OrderShortCodeGenerator.NextSequence(existing, 'A');

        sequence.Should().Be(16);
    }

    [Fact]
    public void NextSequence_Ignora_Codigos_De_Outro_Prefixo()
    {
        var existing = new[] { "B47", "B48" };

        var sequence = OrderShortCodeGenerator.NextSequence(existing, 'A');

        sequence.Should().Be(1, "códigos de outro prefixo (outro dia) não contam para a sequência de hoje");
    }

    [Fact]
    public void BuildCode_Monta_Prefixo_Mais_Sequencia()
    {
        OrderShortCodeGenerator.BuildCode('A', 47).Should().Be("A47");
    }

    [Fact]
    public void ResolvePrefix_E_Determinístico_Para_O_Mesmo_Dia_Operacional()
    {
        var day = new DateOnly(2026, 7, 31);

        var prefix1 = OrderShortCodeGenerator.ResolvePrefix(day);
        var prefix2 = OrderShortCodeGenerator.ResolvePrefix(day);

        prefix1.Should().Be(prefix2);
    }
}
