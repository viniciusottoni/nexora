using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Branding;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Queries.GetPublicMenu;

/// <summary>
/// Cardápio público de um estabelecimento — usado pelo cardápio da mesa/PWA/delivery antes de
/// qualquer login. <c>category</c>/<c>product</c> têm RLS (ADR-004): sem
/// <c>ICurrentTenantContext.TenantId</c> resolvido (este endpoint é <c>[AllowAnonymous]</c>), o
/// banco negaria qualquer leitura por padrão. Por isso o tenant é resolvido primeiro por
/// <see cref="Tenant"/>/<see cref="TenantConfig"/> (tabelas de plataforma, fora do RLS — mesmo
/// caminho de <c>GetPublicBrandingQueryHandler</c>) e só então o contexto é fixado explicitamente
/// via <see cref="IApplicationDbContext.SetTenantContextAsync"/> — o mesmo mecanismo que
/// <c>LoginWithPasswordCommandHandler</c> usa para o trecho de fluxo em que o tenant já é
/// conhecido, mas <c>ICurrentTenantContext</c> ainda não está autenticado.
/// </summary>
internal sealed class GetPublicMenuQueryHandler : IRequestHandler<GetPublicMenuQuery, Result<PublicMenuResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicMenuQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PublicMenuResponse>> Handle(GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        var host = BrandingHost.Normalize(request.Host);

        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Domain != null && t.Domain.ToLower() == host)
            .Select(t => new { t.Id, t.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<PublicMenuResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.PublicMenuTenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var response = await PublicMenuBuilder.BuildAsync(_db, tenant.Id, tenant.Name, request.Channel, cancellationToken);

        return Result<PublicMenuResponse>.Success(response);
    }
}
