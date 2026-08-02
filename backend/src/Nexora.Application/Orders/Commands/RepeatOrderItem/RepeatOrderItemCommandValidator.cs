using FluentValidation;

namespace Nexora.Application.Orders.Commands.RepeatOrderItem;

public sealed class RepeatOrderItemCommandValidator : AbstractValidator<RepeatOrderItemCommand>
{
    public RepeatOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Pedido inválido.");
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item inválido.");
    }
}
