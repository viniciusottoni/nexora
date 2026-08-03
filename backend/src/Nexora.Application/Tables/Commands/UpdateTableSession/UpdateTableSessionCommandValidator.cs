using FluentValidation;

namespace Nexora.Application.Tables.Commands.UpdateTableSession;

public sealed class UpdateTableSessionCommandValidator : AbstractValidator<UpdateTableSessionCommand>
{
    public UpdateTableSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Sessão inválida.");

        RuleFor(x => x)
            .Must(x => x.GuestCount.HasValue || x.WaiterId.HasValue)
            .WithMessage("Informe ao menos a nova contagem de pessoas ou o novo garçom responsável.");

        RuleFor(x => x.GuestCount!.Value)
            .GreaterThan((short)0).WithMessage("Informe quantas pessoas sentaram à mesa.")
            .LessThanOrEqualTo((short)200).WithMessage("Quantidade de pessoas inválida.")
            .When(x => x.GuestCount.HasValue);
    }
}
