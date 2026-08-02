using FluentValidation;

namespace Nexora.Application.Tables.Commands.RequestBillByQr;

/// <summary>Mesma regra de <c>RequestBillCommandValidator</c> (staff) — ver docstring lá.</summary>
public sealed class RequestBillByQrCommandValidator : AbstractValidator<RequestBillByQrCommand>
{
    private static readonly string[] AllowedSplitModes = { "BY_PERSON", "BY_ITEM", "SINGLE" };

    public RequestBillByQrCommandValidator()
    {
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
