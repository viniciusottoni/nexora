using FluentValidation;

namespace Nexora.Application.Tables.Commands.CreateTable;

public sealed class CreateTableCommandValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableCommandValidator()
    {
        RuleFor(x => x.AreaId).NotEmpty().WithMessage("Selecione o ambiente da mesa.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Informe o rótulo da mesa.")
            .MaximumLength(16).WithMessage("O rótulo deve ter no máximo 16 caracteres.");

        RuleFor(x => x.Seats)
            .GreaterThanOrEqualTo((short)1).WithMessage("A mesa precisa ter pelo menos um assento.");
    }
}
