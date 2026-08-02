using FluentValidation;

namespace Awaken.Application.Nutrition.Commands.LogWaterIntake;

public class LogWaterIntakeCommandValidator : AbstractValidator<LogWaterIntakeCommand>
{
    public LogWaterIntakeCommandValidator()
    {
        // US-087 RN-006: valores não-positivos são inválidos.
        RuleFor(x => x.AmountMl)
            .GreaterThan(0).WithMessage("AmountMl must be greater than 0.")
            .LessThanOrEqualTo(5000).WithMessage("AmountMl must not exceed 5000 ml per intake.");
    }
}
