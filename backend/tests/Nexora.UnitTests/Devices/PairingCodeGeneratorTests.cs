using Nexora.Infrastructure.Devices;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Devices;

/// <summary>
/// US-005 §12 ("Unitário: Geração ... do código de pareamento"). Cobre só o formato do código —
/// expiração e uso único são responsabilidade de <see cref="Nexora.Domain.Platform.PairingCode"/>
/// (ver <see cref="PairingCodeTests"/>), não do gerador.
/// </summary>
public sealed class PairingCodeGeneratorTests
{
    private readonly PairingCodeGenerator _sut = new();

    [Fact]
    public void Generate_Produz_Exatamente_Seis_Digitos()
    {
        var code = _sut.Generate();

        code.Should().HaveLength(6);
        code.Should().MatchRegex("^[0-9]{6}$");
    }

    /// <summary>
    /// O intervalo é [0, 1_000_000) formatado como "D6" — precisa incluir o zero-padding (ex.:
    /// "000042"), senão o código "vaza" informação de faixa de valor (códigos curtos nunca
    /// começariam com zero) e o cliente que digita o código teria que adivinhar quantos dígitos
    /// faltam.
    /// </summary>
    [Fact]
    public void Generate_Preserva_Zeros_A_Esquerda()
    {
        // RandomNumberGenerator é CSPRNG — não há como forçar determinismo sem substituir a
        // implementação; em vez disso, gera uma amostra grande e prova estatisticamente que
        // valores pequenos (que exigem padding) aparecem e mantêm 6 caracteres.
        var samples = Enumerable.Range(0, 2000).Select(_ => _sut.Generate()).ToList();

        samples.Should().OnlyContain(code => code.Length == 6);
        samples.Should().Contain(code => code.StartsWith('0'), "com 2000 amostras em [0, 1_000_000), pelo menos uma deveria cair abaixo de 100_000 e exigir padding");
    }

    [Fact]
    public void Generate_Produz_Valores_Distintos_Entre_Chamadas()
    {
        var samples = Enumerable.Range(0, 50).Select(_ => _sut.Generate()).ToList();

        // CSPRNG num espaço de 1 milhão de valores: colisão em 50 amostras é praticamente
        // impossível — se acontecer, é sinal de gerador quebrado (ex.: seed fixo), não de má sorte.
        samples.Distinct().Should().HaveCount(samples.Count);
    }
}
