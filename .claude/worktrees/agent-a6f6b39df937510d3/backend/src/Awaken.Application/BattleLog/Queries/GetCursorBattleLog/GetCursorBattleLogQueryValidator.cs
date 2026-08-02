using FluentValidation;

namespace Awaken.Application.BattleLog.Queries.GetCursorBattleLog;

public class GetCursorBattleLogQueryValidator : AbstractValidator<GetCursorBattleLogQuery>
{
    public GetCursorBattleLogQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50).WithMessage("Limit deve estar entre 1 e 50.");
    }
}
