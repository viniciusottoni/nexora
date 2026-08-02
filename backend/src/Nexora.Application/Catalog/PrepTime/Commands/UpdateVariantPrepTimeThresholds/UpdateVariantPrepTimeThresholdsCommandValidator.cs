using FluentValidation;

namespace Nexora.Application.Catalog.PrepTime.Commands.UpdateVariantPrepTimeThresholds;

/// <summary>
/// Espelha as invariantes de <c>ProductVariant.UpdatePrepTimeThresholds</c> (Domain) aqui na
/// borda — sem isso, uma combinação inválida (ex.: crítico menor que atenção) chegaria ao Domain
/// e lançaria <c>DomainException</c> sem tradutor no pipeline (nenhum behavior a captura hoje),
/// virando 500 em vez do 422 apropriado (ADR-021).
/// </summary>
public sealed class UpdateVariantPrepTimeThresholdsCommandValidator : AbstractValidator<UpdateVariantPrepTimeThresholdsCommand>
{
    public UpdateVariantPrepTimeThresholdsCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("A variação é obrigatória.");

        RuleFor(x => x.PrepMinutes)
            .GreaterThanOrEqualTo((short)0).WithMessage("O tempo de preparo não pode ser negativo.");

        RuleFor(x => x.WarnMinutes)
            .Must((command, warnMinutes) => warnMinutes is null || warnMinutes >= command.PrepMinutes)
            .WithMessage("O limiar de atenção não pode ser menor que o tempo de preparo.")
            .When(x => x.WarnMinutes is not null);

        RuleFor(x => x.CriticalMinutes)
            .Must((command, criticalMinutes) =>
            {
                if (criticalMinutes is null) return true;
                var floor = command.WarnMinutes ?? command.PrepMinutes;
                return criticalMinutes >= floor;
            })
            .WithMessage("O limiar crítico não pode ser menor que o limiar de atenção (ou que o tempo de preparo, se não houver limiar de atenção).")
            .When(x => x.CriticalMinutes is not null);
    }
}
