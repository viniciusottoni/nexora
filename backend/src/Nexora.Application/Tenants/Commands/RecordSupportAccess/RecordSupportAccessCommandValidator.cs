using FluentValidation;

namespace Nexora.Application.Tenants.Commands.RecordSupportAccess;

public sealed class RecordSupportAccessCommandValidator : AbstractValidator<RecordSupportAccessCommand>
{
    public RecordSupportAccessCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Informe o motivo do acesso de suporte.")
            .MaximumLength(500).WithMessage("O motivo deve ter no máximo 500 caracteres.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("A duração do acesso deve ser maior que zero.")
            .LessThanOrEqualTo(480).WithMessage("A duração do acesso não pode ultrapassar 8 horas.");
    }
}
