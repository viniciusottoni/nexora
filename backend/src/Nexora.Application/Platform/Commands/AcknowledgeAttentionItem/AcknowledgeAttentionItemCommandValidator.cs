using FluentValidation;

namespace Nexora.Application.Platform.Commands.AcknowledgeAttentionItem;

public sealed class AcknowledgeAttentionItemCommandValidator : AbstractValidator<AcknowledgeAttentionItemCommand>
{
    public AcknowledgeAttentionItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("O item é obrigatório.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("O motivo é obrigatório.");
    }
}
