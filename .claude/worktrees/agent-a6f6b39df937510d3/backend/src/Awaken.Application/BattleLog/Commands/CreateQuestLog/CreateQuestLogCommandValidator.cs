using FluentValidation;

namespace Awaken.Application.BattleLog.Commands.CreateQuestLog;

public class CreateQuestLogCommandValidator : AbstractValidator<CreateQuestLogCommand>
{
    private static readonly HashSet<string> ValidTypes = ["daily", "dungeon", "raid"];

    public CreateQuestLogCommandValidator()
    {
        RuleFor(x => x.QuestId).NotEmpty();
        RuleFor(x => x.QuestType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("questType deve ser daily, dungeon ou raid.");
        RuleFor(x => x.XpEarned).GreaterThanOrEqualTo(0);
    }
}
