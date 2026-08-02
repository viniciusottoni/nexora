using FluentValidation;

namespace Awaken.Application.BattleLog.Queries.GetPagedBattleLog;

public class GetPagedBattleLogQueryValidator : AbstractValidator<GetPagedBattleLogQuery>
{
    public GetPagedBattleLogQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page deve ser >= 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize deve estar entre 1 e 100.");
    }
}
