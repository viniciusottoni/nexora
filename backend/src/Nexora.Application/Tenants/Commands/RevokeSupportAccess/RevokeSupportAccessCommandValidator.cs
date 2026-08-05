using FluentValidation;

namespace Nexora.Application.Tenants.Commands.RevokeSupportAccess;

public sealed class RevokeSupportAccessCommandValidator : AbstractValidator<RevokeSupportAccessCommand>
{
    public RevokeSupportAccessCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SupportAccessId).NotEmpty();
    }
}
