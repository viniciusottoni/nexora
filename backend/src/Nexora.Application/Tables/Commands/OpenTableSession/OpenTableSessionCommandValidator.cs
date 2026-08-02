using FluentValidation;

namespace Nexora.Application.Tables.Commands.OpenTableSession;

public sealed class OpenTableSessionCommandValidator : AbstractValidator<OpenTableSessionCommand>
{
    public OpenTableSessionCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty().WithMessage("Selecione a mesa a ser aberta.");

        RuleFor(x => x.GuestCount)
            .GreaterThan((short)0).WithMessage("Informe quantas pessoas sentaram à mesa.")
            .LessThanOrEqualTo((short)200).WithMessage("Quantidade de pessoas inválida.");
    }
}
