using FluentValidation;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding.Commands.UpdateBranding;

public sealed class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    public UpdateBrandingCommandValidator()
    {
        RuleFor(x => x.Patch)
            .Must(HasAtLeastOneField).WithMessage("Informe ao menos uma alteração.");

        When(x => x.Patch.Colors is not null, () =>
        {
            RuleFor(x => x.Patch.Colors!.Primary).Matches(HexColorPattern).When(x => x.Patch.Colors!.Primary is not null)
                .WithMessage("Use uma cor no formato #RRGGBB.");
            RuleFor(x => x.Patch.Colors!.Secondary).Matches(HexColorPattern).When(x => x.Patch.Colors!.Secondary is not null)
                .WithMessage("Use uma cor no formato #RRGGBB.");
            RuleFor(x => x.Patch.Colors!.Surface).Matches(HexColorPattern).When(x => x.Patch.Colors!.Surface is not null)
                .WithMessage("Use uma cor no formato #RRGGBB.");
            RuleFor(x => x.Patch.Colors!.OnPrimary).Matches(HexColorPattern).When(x => x.Patch.Colors!.OnPrimary is not null)
                .WithMessage("Use uma cor no formato #RRGGBB.");
        });

        RuleFor(x => x.Patch.Favicon)
            .Must(BeAnHttpsUrl).WithMessage("A mídia deve usar HTTPS.")
            .When(x => x.Patch.Favicon is not null);

        When(x => x.Patch.Logo is not null, () =>
        {
            RuleFor(x => x.Patch.Logo!.Light).Must(BeAnHttpsUrl).When(x => x.Patch.Logo!.Light is not null)
                .WithMessage("A mídia deve usar HTTPS.");
            RuleFor(x => x.Patch.Logo!.Dark).Must(BeAnHttpsUrl).When(x => x.Patch.Logo!.Dark is not null)
                .WithMessage("A mídia deve usar HTTPS.");
        });

        RuleFor(x => x.Patch.Radius)
            .InclusiveBetween(0, 32).WithMessage("O raio deve estar entre 0 e 32.")
            .When(x => x.Patch.Radius is not null);

        When(x => x.Patch.Texts is not null, () =>
        {
            RuleFor(x => x.Patch.Texts!.Welcome).MaximumLength(240).When(x => x.Patch.Texts!.Welcome is not null);
            RuleFor(x => x.Patch.Texts!.OrderConfirmed).MaximumLength(240).When(x => x.Patch.Texts!.OrderConfirmed is not null);
            RuleFor(x => x.Patch.Texts!.Thanks).MaximumLength(240).When(x => x.Patch.Texts!.Thanks is not null);
            RuleFor(x => x.Patch.Texts!.Terms).MaximumLength(20_000).When(x => x.Patch.Texts!.Terms is not null);
        });

        When(x => x.Patch.Pwa is not null, () =>
        {
            RuleFor(x => x.Patch.Pwa!.Name).MaximumLength(45).When(x => x.Patch.Pwa!.Name is not null);
            RuleFor(x => x.Patch.Pwa!.ShortName).MaximumLength(12).When(x => x.Patch.Pwa!.ShortName is not null);
            RuleFor(x => x.Patch.Pwa!.ThemeColor).Matches(HexColorPattern).When(x => x.Patch.Pwa!.ThemeColor is not null)
                .WithMessage("Use uma cor no formato #RRGGBB.");
        });
    }

    private static bool HasAtLeastOneField(UpdateBrandingRequest patch) =>
        patch.Colors is not null || patch.Logo is not null || patch.Favicon is not null ||
        patch.Fonts is not null || patch.Radius is not null || patch.Texts is not null || patch.Pwa is not null;

    private static bool BeAnHttpsUrl(string? value) =>
        value is not null && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
