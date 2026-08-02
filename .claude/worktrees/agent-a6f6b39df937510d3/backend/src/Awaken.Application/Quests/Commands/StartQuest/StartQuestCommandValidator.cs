using FluentValidation;

namespace Awaken.Application.Quests.Commands.StartQuest;

public class StartQuestCommandValidator : AbstractValidator<StartQuestCommand>
{
    public StartQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
