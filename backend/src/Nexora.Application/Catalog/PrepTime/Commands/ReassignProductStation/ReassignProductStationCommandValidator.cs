using FluentValidation;

namespace Nexora.Application.Catalog.PrepTime.Commands.ReassignProductStation;

public sealed class ReassignProductStationCommandValidator : AbstractValidator<ReassignProductStationCommand>
{
    public ReassignProductStationCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O produto é obrigatório.");

        // StationId nulo é válido de propósito — "remover a praça do produto" (RN implícita:
        // produto pode ficar sem praça definida até alguém atribuir uma).
    }
}
