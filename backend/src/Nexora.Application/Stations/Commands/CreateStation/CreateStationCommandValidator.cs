using FluentValidation;

namespace Nexora.Application.Stations.Commands.CreateStation;

public sealed class CreateStationCommandValidator : AbstractValidator<CreateStationCommand>
{
    public CreateStationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Informe um código.")
            .MaximumLength(32).WithMessage("O código deve ter no máximo 32 caracteres.")
            .Matches("^[A-Z][A-Z0-9_]*$").WithMessage("Use letras maiúsculas, números e sublinhado.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe um nome.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        // Chave semântica da paleta (ADR-010: nenhum componente usa cor literal) — nunca hex cru;
        // o token CSS real fica só em packages/ui/src/tokens/colors.css.
        RuleFor(x => x.Color)
            .MaximumLength(16).WithMessage("A cor deve ter no máximo 16 caracteres.")
            .Matches("^[a-z][a-z-]*$").WithMessage("Escolha uma cor da paleta permitida.")
            .When(x => x.Color is not null);

        // US-017 §12 ("Unitário: validação de ... capacidade positiva") — capacity_slots é o
        // número de posições simultâneas do recurso; zero ou negativo não descreve um recurso real.
        RuleFor(x => x.CapacitySlots)
            .GreaterThan((short)0).WithMessage("A capacidade deve ser maior que zero.")
            .When(x => x.CapacitySlots.HasValue);

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo((short)0).WithMessage("A posição não pode ser negativa.");
    }
}
