using FluentValidation;

namespace Nexora.Application.Branding.Commands.PrepareBrandingUpload;

public sealed class PrepareBrandingUploadCommandValidator : AbstractValidator<PrepareBrandingUploadCommand>
{
    private static readonly string[] KnownKinds = { "LOGO_LIGHT", "LOGO_DARK", "FAVICON", "PWA_ICON" };
    private static readonly string[] KnownContentTypes = { "image/svg+xml", "image/png", "image/jpeg", "image/webp" };
    private static readonly string[] RasterOnlyKinds = { "FAVICON", "PWA_ICON" };

    public PrepareBrandingUploadCommandValidator()
    {
        RuleFor(x => x.Kind)
            .Must(KnownKinds.Contains).WithMessage("Tipo de mídia de marca desconhecido.");

        RuleFor(x => x.ContentType)
            .Must(KnownContentTypes.Contains).WithMessage("Tipo de arquivo não suportado.");

        RuleFor(x => x.Bytes)
            .GreaterThan(0).WithMessage("Arquivo vazio.")
            .LessThanOrEqualTo(10_000_000).WithMessage("O arquivo deve ter no máximo 10 MB.");

        RuleFor(x => x.Sha256)
            .Matches("^[0-9a-fA-F]{64}$").WithMessage("Hash SHA-256 inválido.");

        RuleFor(x => x)
            .Must(x => !(RasterOnlyKinds.Contains(x.Kind) && x.ContentType == "image/svg+xml"))
            .WithMessage("Ícones PWA devem ser rasterizados.")
            .OverridePropertyName("contentType");
    }
}
