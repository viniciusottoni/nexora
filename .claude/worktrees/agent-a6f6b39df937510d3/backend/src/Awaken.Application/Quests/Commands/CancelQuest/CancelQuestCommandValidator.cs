using FluentValidation;

namespace Awaken.Application.Quests.Commands.CancelQuest;

public class CancelQuestCommandValidator : AbstractValidator<CancelQuestCommand>
{
    public CancelQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
