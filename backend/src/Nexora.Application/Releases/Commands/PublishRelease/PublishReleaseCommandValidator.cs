using FluentValidation;

namespace Nexora.Application.Releases.Commands.PublishRelease;

public sealed class PublishReleaseCommandValidator : AbstractValidator<PublishReleaseCommand>
{
    public PublishReleaseCommandValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("A versão da release é obrigatória.")
            .MaximumLength(20).WithMessage("A versão da release deve ter no máximo 20 caracteres.");

        RuleFor(x => x.RolloutPercent)
            .InclusiveBetween(0, 100).WithMessage("O percentual de liberação deve estar entre 0 e 100.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("As notas da release devem ter no máximo 2000 caracteres.");
    }
}
