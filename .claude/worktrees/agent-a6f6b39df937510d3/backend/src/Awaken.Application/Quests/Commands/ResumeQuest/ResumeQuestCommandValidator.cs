using FluentValidation;

namespace Awaken.Application.Quests.Commands.ResumeQuest;

public class ResumeQuestCommandValidator : AbstractValidator<ResumeQuestCommand>
{
    public ResumeQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
