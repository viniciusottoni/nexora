using FluentValidation;

namespace Nexora.Application.Cashier.Commands.ApplyDiscount;

public sealed class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    private static readonly string[] AllowedScopes = { "SESSION", "ITEM" };

    public ApplyDiscountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para aplicar o desconto.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("O motivo do desconto é obrigatório.");
        RuleFor(x => x.Scope)
            .NotEmpty().WithMessage("Informe o escopo do desconto.")
            .Must(s => AllowedScopes.Contains(s.Trim().ToUpperInvariant())).WithMessage("Escopo inválido — use SESSION ou ITEM.");
        RuleFor(x => x.OrderItemId)
            .NotNull().WithMessage("Informe o item para desconto por item.")
            .When(x => x.Scope.Trim().Equals("ITEM", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x)
            .Must(x => x.Percent.HasValue || x.Amount.HasValue)
            .WithMessage("Informe o percentual ou o valor do desconto.");
        RuleFor(x => x)
            .Must(x => !(x.Percent.HasValue && x.Amount.HasValue))
            .WithMessage("Informe apenas o percentual ou o valor do desconto.");
        RuleFor(x => x.Percent)
            .InclusiveBetween(0m, 100m)
            .When(x => x.Percent.HasValue)
            .WithMessage("O percentual de desconto deve estar entre 0 e 100.");
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.Amount.HasValue)
            .WithMessage("O valor do desconto não pode ser negativo.");
    }
}
