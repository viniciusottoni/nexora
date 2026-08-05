using System.Text.RegularExpressions;
using FluentValidation;

namespace Nexora.Application.TenantDomains.Commands.RegisterTenantDomain;

public sealed class RegisterTenantDomainCommandValidator : AbstractValidator<RegisterTenantDomainCommand>
{
    // Hostname RFC 1123: rótulos de 1-63 caracteres (letras/dígitos/hífen, sem hífen nas pontas),
    // pelo menos um ponto — mesmo limite de tamanho de coluna do banco (253, tenant_domain.domain).
    private static readonly Regex HostnamePattern = new(
        @"^(?=.{1,253}$)(?!-)[a-zA-Z0-9-]{1,63}(?<!-)(\.(?!-)[a-zA-Z0-9-]{1,63}(?<!-))+$",
        RegexOptions.Compiled);

    public RegisterTenantDomainCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();

        RuleFor(c => c.Domain)
            .NotEmpty().WithMessage("O domínio é obrigatório.")
            .MaximumLength(253).WithMessage("O domínio não pode ter mais de 253 caracteres.")
            .Must(domain => HostnamePattern.IsMatch(domain.Trim()))
            .WithMessage("Informe um domínio válido (ex.: cardapio.seudominio.com.br).");
    }
}
