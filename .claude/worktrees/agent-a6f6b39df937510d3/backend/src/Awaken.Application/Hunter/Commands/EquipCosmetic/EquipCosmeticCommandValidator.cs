using FluentValidation;

namespace Awaken.Application.Hunter.Commands.EquipCosmetic;

public class EquipCosmeticCommandValidator : AbstractValidator<EquipCosmeticCommand>
{
    private static readonly string[] ValidSlots = ["frame", "aura", "background"];

    public EquipCosmeticCommandValidator()
    {
        RuleFor(x => x.Slot).NotEmpty().Must(slot => ValidSlots.Contains(slot));
        RuleFor(x => x.ItemKey).MaximumLength(64);
    }
}
