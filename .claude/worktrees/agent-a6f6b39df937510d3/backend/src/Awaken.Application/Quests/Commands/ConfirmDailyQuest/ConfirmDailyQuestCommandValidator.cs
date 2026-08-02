using FluentValidation;

namespace Awaken.Application.Quests.Commands.ConfirmDailyQuest;

public class ConfirmDailyQuestCommandValidator : AbstractValidator<ConfirmDailyQuestCommand>
{
    public ConfirmDailyQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
