using FluentValidation;

namespace Nexora.Application.Orders.Commands.CancelOrderItem;

/// <summary>US-033 §4/§10 — motivo obrigatório (lista curta e configurável no cliente); observação livre opcional.</summary>
public sealed class CancelOrderItemCommandValidator : AbstractValidator<CancelOrderItemCommand>
{
    public CancelOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Pedido inválido.");
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item inválido.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("O motivo do cancelamento é obrigatório.")
            .MaximumLength(120).WithMessage("Motivo inválido.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Observação muito longa.");
    }
}
