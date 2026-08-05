using FluentValidation;

namespace Nexora.Application.Cashier.Commands.RegisterPayments;

public sealed class RegisterPaymentsCommandValidator : AbstractValidator<RegisterPaymentsCommand>
{
    private static readonly string[] AllowedMethods = { "CASH", "CREDIT", "DEBIT", "PIX", "ONLINE", "VOUCHER", "OTHER" };

    public RegisterPaymentsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para registrar o pagamento.");
        RuleFor(x => x.Payments).NotEmpty().WithMessage("Informe ao menos uma forma de pagamento.");

        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(p => p.Amount).GreaterThan(0m).WithMessage("O valor de cada pagamento precisa ser maior que zero.");
            payment.RuleFor(p => p.Method)
                .NotEmpty().WithMessage("Informe a forma de pagamento.")
                .Must(m => AllowedMethods.Contains(m.Trim().ToUpperInvariant())).WithMessage("Forma de pagamento inválida.");
            payment.RuleFor(p => p.Installments).GreaterThanOrEqualTo(1).WithMessage("O número de parcelas deve ser pelo menos 1.");
            payment.RuleFor(p => p.ReceivedAmount)
                .GreaterThanOrEqualTo(p => p.Amount)
                .When(p => p.ReceivedAmount.HasValue)
                .WithMessage("O valor recebido não pode ser menor que o valor do pagamento.");
        });
    }
}
