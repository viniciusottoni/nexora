using FluentValidation;

namespace Nexora.Application.Catalog.FractionPricing.Queries.PreviewFractionPricing;

/// <summary>
/// Validação estrutural (FluentValidation) — o mínimo de duas frações e a checagem de
/// compatibilidade (tamanho/grupo/soma de pesos) são regra de NEGÓCIO e ficam em
/// <see cref="FractionPricingCalculator"/> (que devolve o código específico de
/// <c>Nexora.Shared.Errors.ApiErrorCodes</c> por cenário); aqui só se recusa entrada
/// estruturalmente inválida (lista vazia, variante não informada, peso fora de faixa) antes de
/// qualquer consulta ao banco.
/// </summary>
public sealed class PreviewFractionPricingQueryValidator : AbstractValidator<PreviewFractionPricingQuery>
{
    public PreviewFractionPricingQueryValidator()
    {
        RuleFor(x => x.Fractions)
            .NotEmpty().WithMessage("Informe ao menos duas frações.");

        RuleForEach(x => x.Fractions).ChildRules(fraction =>
        {
            fraction.RuleFor(f => f.VariantId)
                .NotEmpty().WithMessage("Selecione uma variante para cada fração.");

            fraction.RuleFor(f => f.Weight)
                .GreaterThan(0m).WithMessage("O peso da fração deve ser maior que zero.")
                .LessThanOrEqualTo(1m).WithMessage("O peso da fração não pode ultrapassar 1,0.");
        });
    }
}
