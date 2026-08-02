using FluentValidation;

namespace Awaken.Application.Nutrition.Commands.UpdateCupVolume;

public class UpdateCupVolumeCommandValidator : AbstractValidator<UpdateCupVolumeCommand>
{
    public UpdateCupVolumeCommandValidator()
    {
        // US-090 RN-005: valores válidos entre 50 e 2000 ml.
        RuleFor(x => x.CupVolumeMl)
            .InclusiveBetween(50, 2000)
            .WithMessage("CupVolumeMl must be between 50 and 2000 ml.");
    }
}
