using FluentValidation;

namespace Nexora.Application.Stations.Commands.UpdateStation;

public sealed class UpdateStationCommandValidator : AbstractValidator<UpdateStationCommand>
{
    public UpdateStationCommandValidator()
    {
        RuleFor(x => x.StationId)
            .NotEmpty().WithMessage("Praça não identificada.");

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Color is not null || x.CapacitySlots is not null
                       || x.IsBottleneck is not null || x.Position is not null)
            .WithMessage("Informe ao menos uma alteração.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome não pode ficar vazio.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.")
            .When(x => x.Name is not null);

        // Chave semântica da paleta (ADR-010: nenhum componente usa cor literal) — nunca hex cru;
        // o token CSS real fica só em packages/ui/src/tokens/colors.css.
        RuleFor(x => x.Color)
            .MaximumLength(16).WithMessage("A cor deve ter no máximo 16 caracteres.")
            .Matches("^[a-z][a-z-]*$").WithMessage("Escolha uma cor da paleta permitida.")
            .When(x => x.Color is not null);

        // US-017 §12 — mesma regra de capacidade positiva da criação.
        RuleFor(x => x.CapacitySlots)
            .GreaterThan((short)0).WithMessage("A capacidade deve ser maior que zero.")
            .When(x => x.CapacitySlots.HasValue);

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo((short)0).WithMessage("A posição não pode ser negativa.")
            .When(x => x.Position.HasValue);
    }
}
