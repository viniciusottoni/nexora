using Nexora.Domain.Platform;
using FluentValidation;

namespace Nexora.Application.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Papel não identificado.");

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Permissions is not null)
            .WithMessage("Informe ao menos uma alteração.");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Permissions)
            .Must(p => p!.Count <= PermissionCatalog.AllCodes.Count).WithMessage("Permissões inválidas.")
            .When(x => x.Permissions is not null);

        RuleForEach(x => x.Permissions)
            .Must(code => PermissionCatalog.AllCodes.Contains(code)).WithMessage("Permissão desconhecida.")
            .When(x => x.Permissions is not null);
    }
}
