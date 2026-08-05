using FluentAssertions;
using Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;
using Nexora.Contracts.Catalog;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-044 §10: "motivo escolhido por número (1 acabou, 2 equipamento, 3 qualidade), não por
/// texto" — prova que <see cref="MarkProductUnavailableCommandValidator"/> recusa qualquer valor
/// fora da lista curta e fixa de <see cref="ProductUnavailableReasons"/> (a US-015 original aceitava
/// texto livre; esta suíte fecha essa lacuna). Não precisa de banco — mesmo padrão de
/// <c>CreateVariantCommandValidatorTests</c>.
/// </summary>
public sealed class MarkProductUnavailableCommandValidatorTests
{
    private readonly MarkProductUnavailableCommandValidator _validator = new();

    [Theory]
    [InlineData(ProductUnavailableReasons.OutOfStock)]
    [InlineData(ProductUnavailableReasons.Equipment)]
    [InlineData(ProductUnavailableReasons.Quality)]
    public void Aceita_Os_Tres_Motivos_Fixos_Da_Lista_Numerada(string reason)
    {
        var command = new MarkProductUnavailableCommand(Guid.NewGuid(), reason);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Acabou a calabresa")]
    [InlineData("out_of_stock")]
    [InlineData("QUALQUER_TEXTO_LIVRE")]
    public void Recusa_Texto_Livre_Fora_Da_Lista_Numerada(string reason)
    {
        var command = new MarkProductUnavailableCommand(Guid.NewGuid(), reason);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Reason));
    }

    [Fact]
    public void Recusa_Motivo_Vazio_Com_A_Mensagem_De_Motivo_Obrigatorio_Nao_A_De_Lista_Invalida()
    {
        var command = new MarkProductUnavailableCommand(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(command.Reason))
            .Which.ErrorMessage.Should().Be("Informe o motivo da indisponibilidade.");
    }
}
