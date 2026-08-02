using Nexora.Domain.Provisioning;
using FluentValidation;

namespace Nexora.Application.Tenants.Commands.ProvisionTenant;

public sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe o nome do estabelecimento.")
            .MaximumLength(120).WithMessage("O nome deve ter no máximo 120 caracteres.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Informe o endereço do estabelecimento.")
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Use apenas letras minúsculas, números e hífen.")
            .MaximumLength(63).WithMessage("O endereço deve ter no máximo 63 caracteres.");

        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Informe o plano contratado.");

        RuleFor(x => x.Template)
            .NotEmpty().WithMessage("Informe o modelo de negócio.")
            .Must(ProvisioningTemplates.IsKnown).WithMessage("Modelo de negócio desconhecido.");

        RuleFor(x => x.OwnerName)
            .NotEmpty().WithMessage("Informe o nome do proprietário.");

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().WithMessage("Informe o e-mail do proprietário.")
            .EmailAddress().WithMessage("Informe um e-mail válido.");

        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("Informe o nome da loja.");

        RuleFor(x => x.StoreTimezone)
            .NotEmpty().WithMessage("Informe o fuso horário da loja.");
    }
}
