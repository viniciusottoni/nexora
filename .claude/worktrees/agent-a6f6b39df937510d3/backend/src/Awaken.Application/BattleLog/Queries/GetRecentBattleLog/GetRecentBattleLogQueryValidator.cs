using FluentValidation;

namespace Awaken.Application.BattleLog.Queries.GetRecentBattleLog;

public class GetRecentBattleLogQueryValidator : AbstractValidator<GetRecentBattleLogQuery>
{
    public GetRecentBattleLogQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit deve estar entre 1 e 100.");
    }
}
