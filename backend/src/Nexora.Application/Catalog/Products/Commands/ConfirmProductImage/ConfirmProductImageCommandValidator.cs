using FluentValidation;

namespace Nexora.Application.Catalog.Products.Commands.ConfirmProductImage;

public sealed class ConfirmProductImageCommandValidator : AbstractValidator<ConfirmProductImageCommand>
{
    private static readonly string[] KnownContentTypes = { "image/png", "image/jpeg", "image/webp", "image/heic", "image/heif" };

    public ConfirmProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produto inválido.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Informe a URL do arquivo enviado.")
            .MaximumLength(2048);

        RuleFor(x => x.ContentType)
            .Must(KnownContentTypes.Contains).WithMessage("Tipo de arquivo não suportado. Envie PNG, JPEG, WebP ou HEIC.");

        RuleFor(x => x.Bytes)
            .GreaterThan(0).WithMessage("Arquivo vazio.")
            .LessThanOrEqualTo(10_000_000).WithMessage("O arquivo deve ter no máximo 10 MB.");

        RuleFor(x => x.Sha256)
            .Matches("^[0-9a-fA-F]{64}$").WithMessage("Hash SHA-256 inválido.");

        RuleFor(x => x.Width)
            .NotNull().WithMessage("Informe a largura da imagem.")
            .GreaterThanOrEqualTo(800).WithMessage("A imagem deve ter pelo menos 800 px de largura.");
        RuleFor(x => x.Height)
            .NotNull().WithMessage("Informe a altura da imagem.")
            .GreaterThanOrEqualTo(600).WithMessage("A imagem deve ter pelo menos 600 px de altura.");
    }
}
