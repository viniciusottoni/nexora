using Nexora.Infrastructure.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-020 §12 ("Unitário: Geração de token com entropia adequada e sem sequência previsível") e
/// cenário Gherkin "Token não adivinhável". Diferente de <c>PairingCodeGeneratorTests</c> (espaço
/// de só 1 milhão de valores, aceitável porque é digitado por humano e tem expiração curta), o
/// <c>qr_token</c> é um segredo de entrada de longa duração — precisa de entropia muito maior.
/// </summary>
public sealed class QrTokenGeneratorTests
{
    private readonly QrTokenGenerator _sut = new();

    [Fact]
    public void Generate_Produz_Token_Nao_Vazio_E_Url_Safe()
    {
        var token = _sut.Generate();

        token.Should().NotBeNullOrWhiteSpace();
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$", "o token vira parte de uma URL/QR Code — não pode conter '+', '/' ou '=' de Base64 padrão");
    }

    /// <summary>256 bits de entropia (32 bytes) — bem acima do mínimo de 128 bits recomendado para segredo de entrada não adivinhável por força bruta.</summary>
    [Fact]
    public void Generate_Produz_Entropia_De_256_Bits()
    {
        var token = _sut.Generate();

        // Base64Url de 32 bytes sem padding: ceil(32 * 4 / 3) = 43 caracteres.
        token.Length.Should().Be(43);
    }

    [Fact]
    public void Generate_Produz_Valores_Distintos_Entre_Chamadas()
    {
        var samples = Enumerable.Range(0, 500).Select(_ => _sut.Generate()).ToList();

        // CSPRNG de 256 bits: colisão em 500 amostras é praticamente impossível — se acontecer, é
        // sinal de gerador quebrado (ex.: seed fixo ou reaproveitamento de estado).
        samples.Distinct().Should().HaveCount(samples.Count);
    }

    /// <summary>
    /// Cenário Gherkin "Token não adivinhável": não existe relação previsível entre o token de uma
    /// mesa e o de outra — o token não é derivado de um contador/índice sequencial. Prova
    /// negativa: converte uma amostra grande para bytes e verifica que o primeiro byte (que
    /// carregaria qualquer contador sequencial de baixa magnitude) se distribui, em vez de
    /// incrementar previsivelmente.
    /// </summary>
    [Fact]
    public void Generate_Nao_Tem_Sequencia_Previsivel_Entre_Tokens_Consecutivos()
    {
        var tokens = Enumerable.Range(0, 200).Select(_ => _sut.Generate()).ToList();

        var firstCharacters = tokens.Select(t => t[0]).Distinct().ToList();

        // Um contador sequencial (ex.: mesa 12 -> mesa 13) produziria um prefixo quase idêntico
        // entre tokens consecutivos; um CSPRNG de 256 bits produz primeiros caracteres
        // amplamente distribuídos mesmo numa amostra pequena.
        firstCharacters.Count.Should().BeGreaterThan(10);
    }
}
