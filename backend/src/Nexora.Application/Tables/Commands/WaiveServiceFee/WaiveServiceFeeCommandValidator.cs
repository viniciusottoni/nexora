using FluentValidation;

namespace Nexora.Application.Tables.Commands.WaiveServiceFee;

public sealed class WaiveServiceFeeCommandValidator : AbstractValidator<WaiveServiceFeeCommand>
{
    public WaiveServiceFeeCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para retirar a taxa de serviço.");
        RuleFor(x => x.People).GreaterThan((short)0).WithMessage("A quantidade de pessoas precisa ser maior que zero.");
        RuleFor(x => x.Person)
            .GreaterThan(0).WithMessage("O número da pessoa precisa ser maior que zero.")
            .LessThanOrEqualTo(x => x.People).WithMessage("O número da pessoa não pode ser maior que a quantidade de pessoas na divisão.");
    }
}
