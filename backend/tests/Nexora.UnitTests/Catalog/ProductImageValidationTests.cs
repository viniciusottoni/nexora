using FluentAssertions;
using Nexora.Application.Catalog.Products.Commands.ConfirmProductImage;
using Nexora.Application.Catalog.Products.Commands.PrepareProductImageUpload;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class ProductImageValidationTests
{
    private const string Sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Deve_Aceitar_Heic_Ate_Dez_Megabytes()
    {
        var command = new PrepareProductImageUploadCommand(Guid.NewGuid(), "image/heic", 10_000_000, Sha256);

        var result = new PrepareProductImageUploadCommandValidator().Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(799, 600)]
    [InlineData(800, 599)]
    public void Deve_Rejeitar_Imagem_Abaixo_De_Oitocentos_Por_Seiscentos(int width, int height)
    {
        var command = new ConfirmProductImageCommand(
            Guid.NewGuid(), "https://cdn.example/photo.jpg", "image/jpeg", 1_000, Sha256, width, height);

        var result = new ConfirmProductImageCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
