using FluentValidation;

namespace Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;

public sealed class MarkProductUnavailableCommandValidator : AbstractValidator<MarkProductUnavailableCommand>
{
    public MarkProductUnavailableCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Selecione um produto.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Informe o motivo da indisponibilidade.")
            .MaximumLength(200).WithMessage("O motivo deve ter no máximo 200 caracteres.");
    }
}
