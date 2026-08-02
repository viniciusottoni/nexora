using FluentValidation;

namespace Nexora.Application.Installations.Queries.GetInitialSyncPage;

public sealed class GetInitialSyncPageQueryValidator : AbstractValidator<GetInitialSyncPageQuery>
{
    public GetInitialSyncPageQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.InstallationId).NotEmpty();
        RuleFor(x => x.Cursor).GreaterThanOrEqualTo(0).WithMessage("Cursor de sincronização inválido.");
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 500)
            .WithMessage("O limite de página de sincronização deve estar entre 1 e 500.");
    }
}
