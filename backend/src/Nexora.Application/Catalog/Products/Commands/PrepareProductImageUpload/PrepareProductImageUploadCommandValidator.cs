using FluentValidation;

namespace Nexora.Application.Catalog.Products.Commands.PrepareProductImageUpload;

public sealed class PrepareProductImageUploadCommandValidator : AbstractValidator<PrepareProductImageUploadCommand>
{
    private static readonly string[] KnownContentTypes = { "image/png", "image/jpeg", "image/webp", "image/heic", "image/heif" };

    public PrepareProductImageUploadCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produto inválido.");

        RuleFor(x => x.ContentType)
            .Must(KnownContentTypes.Contains).WithMessage("Tipo de arquivo não suportado. Envie PNG, JPEG, WebP ou HEIC.");

        RuleFor(x => x.Bytes)
            .GreaterThan(0).WithMessage("Arquivo vazio.")
            .LessThanOrEqualTo(10_000_000).WithMessage("O arquivo deve ter no máximo 10 MB.");

        RuleFor(x => x.Sha256)
            .Matches("^[0-9a-fA-F]{64}$").WithMessage("Hash SHA-256 inválido.");
    }
}
