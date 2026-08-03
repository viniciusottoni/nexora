using FluentValidation;

namespace Nexora.Application.Tables.Commands.UpdateTable;

public sealed class UpdateTableCommandValidator : AbstractValidator<UpdateTableCommand>
{
    public UpdateTableCommandValidator()
    {
        RuleFor(x => x.AreaId).NotEmpty().WithMessage("Selecione o ambiente da mesa.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Informe o rótulo da mesa.")
            .MaximumLength(16).WithMessage("O rótulo deve ter no máximo 16 caracteres.");

        RuleFor(x => x.Seats)
            .GreaterThanOrEqualTo((short)1).WithMessage("A mesa precisa ter pelo menos um assento.");
    }
}
