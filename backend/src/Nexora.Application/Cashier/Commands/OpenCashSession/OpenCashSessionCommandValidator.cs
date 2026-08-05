using FluentValidation;

namespace Nexora.Application.Cashier.Commands.OpenCashSession;

public sealed class OpenCashSessionCommandValidator : AbstractValidator<OpenCashSessionCommand>
{
    public OpenCashSessionCommandValidator()
    {
        RuleFor(x => x.OpeningAmount)
            .GreaterThanOrEqualTo(0m).WithMessage("O fundo de caixa não pode ser negativo.");
    }
}
