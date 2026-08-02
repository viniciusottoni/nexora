using FluentValidation;

namespace Awaken.Application.Quests.Queries.GetQuestRewardSummary;

public class GetQuestRewardSummaryQueryValidator : AbstractValidator<GetQuestRewardSummaryQuery>
{
    public GetQuestRewardSummaryQueryValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");
    }
}
