using FluentValidation;

namespace Nexora.Application.Cashier.Commands.CloseCashSession;

public sealed class CloseCashSessionCommandValidator : AbstractValidator<CloseCashSessionCommand>
{
    public CloseCashSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a sessão de caixa a fechar.");
        RuleFor(x => x.CountedAmount).GreaterThanOrEqualTo(0m).WithMessage("O valor contado não pode ser negativo.");
    }
}
