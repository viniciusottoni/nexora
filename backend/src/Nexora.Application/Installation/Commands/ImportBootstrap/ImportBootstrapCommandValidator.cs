using FluentValidation;

namespace Nexora.Application.Installation.Commands.ImportBootstrap;

public sealed class ImportBootstrapCommandValidator : AbstractValidator<ImportBootstrapCommand>
{
    public ImportBootstrapCommandValidator()
    {
        RuleFor(x => x.Tenant.Id).NotEmpty().WithMessage("O id do tenant é obrigatório.");
        RuleFor(x => x.Tenant.Name).NotEmpty().WithMessage("O nome do tenant é obrigatório.");
        RuleFor(x => x.Tenant.Slug).NotEmpty().WithMessage("O slug do tenant é obrigatório.");

        RuleFor(x => x.Store.Id).NotEmpty().WithMessage("O id da loja é obrigatório.");
        RuleFor(x => x.Store.Name).NotEmpty().WithMessage("O nome da loja é obrigatório.");
        RuleFor(x => x.Store.Timezone).NotEmpty().WithMessage("O fuso horário da loja é obrigatório.");

        RuleFor(x => x.Installation.Id).NotEmpty().WithMessage("O id da instalação edge é obrigatório.");
        RuleFor(x => x.Installation.PublicKey)
            .NotEmpty().WithMessage("A chave pública da instalação edge é obrigatória.")
            .MinimumLength(32).WithMessage("A chave pública da instalação edge é inválida.");
        RuleFor(x => x.Installation.Version).NotEmpty().WithMessage("A versão do edge é obrigatória.");

        RuleFor(x => x.ConfigPages)
            .NotEmpty().WithMessage("A carga de configuração inicial é obrigatória.");
    }
}
