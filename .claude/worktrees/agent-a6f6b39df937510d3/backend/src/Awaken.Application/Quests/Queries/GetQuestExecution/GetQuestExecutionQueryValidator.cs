using FluentValidation;

namespace Awaken.Application.Quests.Queries.GetQuestExecution;

public class GetQuestExecutionQueryValidator : AbstractValidator<GetQuestExecutionQuery>
{
    public GetQuestExecutionQueryValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
