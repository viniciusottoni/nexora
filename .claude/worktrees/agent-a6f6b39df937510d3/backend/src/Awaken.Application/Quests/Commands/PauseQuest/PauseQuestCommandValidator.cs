using FluentValidation;

namespace Awaken.Application.Quests.Commands.PauseQuest;

public class PauseQuestCommandValidator : AbstractValidator<PauseQuestCommand>
{
    public PauseQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
