using Awaken.Domain.Entities.Quests;
using FluentValidation;

namespace Awaken.Application.Quests.Commands.CompleteQuest;

public class CompleteQuestCommandValidator : AbstractValidator<CompleteQuestCommand>
{
    public CompleteQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");

        RuleFor(x => x.PerceivedFeeling)
            .Must(PerceivedFeelings.IsValid)
            .WithMessage("PerceivedFeeling deve ser too_easy, just_right, too_hard ou nulo.");
    }
}
