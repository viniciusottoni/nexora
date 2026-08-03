using FluentValidation;

namespace Nexora.Application.Tables.Commands.RegisterPartialPayment;

public sealed class RegisterPartialPaymentCommandValidator : AbstractValidator<RegisterPartialPaymentCommand>
{
    private static readonly string[] AllowedMethods = { "CASH", "CREDIT", "DEBIT", "PIX", "ONLINE", "VOUCHER", "OTHER" };

    public RegisterPartialPaymentCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para registrar o pagamento.");
        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("O valor pago precisa ser maior que zero.");
        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Informe a forma de pagamento.")
            .Must(m => AllowedMethods.Contains(m.Trim().ToUpperInvariant())).WithMessage("Forma de pagamento inválida.");
    }
}
