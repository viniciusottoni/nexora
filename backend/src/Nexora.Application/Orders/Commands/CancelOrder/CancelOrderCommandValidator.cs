using FluentValidation;

namespace Nexora.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Pedido inválido.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("O motivo do cancelamento é obrigatório.")
            .MaximumLength(120).WithMessage("Motivo inválido.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Observação muito longa.");
    }
}
