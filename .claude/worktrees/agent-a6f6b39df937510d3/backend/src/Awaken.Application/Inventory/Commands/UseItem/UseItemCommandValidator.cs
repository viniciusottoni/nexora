using FluentValidation;

namespace Awaken.Application.Inventory.Commands.UseItem;

public class UseItemCommandValidator : AbstractValidator<UseItemCommand>
{
    public UseItemCommandValidator()
    {
        RuleFor(x => x.ItemKey)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.UseRequestId)
            .NotEmpty()
            .MaximumLength(36);

        RuleFor(x => x.ContextType)
            .MaximumLength(50)
            .When(x => x.ContextType is not null);
    }
}
