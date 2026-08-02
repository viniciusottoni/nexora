using FluentValidation;

namespace Nexora.Application.Installations.Queries.GetInstallationSyncHealth;

public sealed class GetInstallationSyncHealthQueryValidator : AbstractValidator<GetInstallationSyncHealthQuery>
{
    public GetInstallationSyncHealthQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.InstallationId).NotEmpty();
    }
}
