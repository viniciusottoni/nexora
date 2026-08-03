using FluentValidation;

namespace Nexora.Application.Tables.Commands.RequestBill;

/// <summary>US-026 §7: <c>splitMode</c> é um de três valores fechados; <c>people</c> só é exigido/relevante em <c>BY_PERSON</c>.</summary>
public sealed class RequestBillCommandValidator : AbstractValidator<RequestBillCommand>
{
    private static readonly string[] AllowedSplitModes = { "BY_PERSON", "BY_ITEM", "SINGLE" };

    public RequestBillCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para pedir a conta.");

        RuleFor(x => x.SplitMode)
            .NotEmpty().WithMessage("Escolha como a conta será dividida.")
            .Must(mode => AllowedSplitModes.Contains(mode))
            .WithMessage("Modo de divisão inválido. Escolha por pessoa, por item ou valor único.");

        RuleFor(x => x.People)
            .NotNull().WithMessage("Informe quantas pessoas vão dividir a conta.")
            .GreaterThan((short)0).WithMessage("A quantidade de pessoas precisa ser maior que zero.")
            .When(x => x.SplitMode == "BY_PERSON");
    }
}
