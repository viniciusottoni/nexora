using FluentValidation;

namespace Nexora.Application.Cashier.Commands.RegisterCashMovement;

public sealed class RegisterCashMovementCommandValidator : AbstractValidator<RegisterCashMovementCommand>
{
    private static readonly string[] AllowedTypes = { "WITHDRAWAL", "SUPPLY" };

    public RegisterCashMovementCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Informe se é sangria ou suprimento.")
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage("Tipo de movimento inválido. Use sangria (retirada) ou suprimento (entrada).");

        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("O valor do movimento deve ser maior que zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Informe o motivo do movimento.")
            .MaximumLength(500).WithMessage("O motivo pode ter no máximo 500 caracteres.");
    }
}
