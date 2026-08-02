using Nexora.Domain.Platform;
using FluentValidation;

namespace Nexora.Application.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Informe um código.")
            .MaximumLength(32).WithMessage("O código deve ter no máximo 32 caracteres.")
            .Matches("^[A-Z][A-Z0-9_]*$").WithMessage("Use letras maiúsculas, números e sublinhado.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe um nome.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Permissions)
            .Must(p => p.Count <= PermissionCatalog.AllCodes.Count).WithMessage("Permissões inválidas.");

        RuleForEach(x => x.Permissions)
            .Must(code => PermissionCatalog.AllCodes.Contains(code)).WithMessage("Permissão desconhecida.");
    }
}
