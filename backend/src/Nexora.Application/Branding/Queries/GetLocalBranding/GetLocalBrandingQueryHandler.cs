using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Branding;
using Nexora.Contracts.Branding;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Branding.Queries.GetLocalBranding;

internal sealed class GetLocalBrandingQueryHandler : IRequestHandler<GetLocalBrandingQuery, Result<BrandingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetLocalBrandingQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<BrandingResponse>> Handle(GetLocalBrandingQuery request, CancellationToken cancellationToken)
    {
        // Defensivo: em Nexora.Api.Edge, EdgeCurrentTenantContext.TenantId vem de
        // EdgeInstallationOptions e nunca é nulo em operação normal (ADR-004) — mas uma instalação
        // sem "Edge:Installation:TenantId" configurado ainda pode chegar aqui, e falhar fechado
        // (em vez de estourar NullReferenceException) é o comportamento esperado do ADR-004.
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Result<BrandingResponse>.Failure(
                "Não foi possível identificar o estabelecimento desta instalação.",
                ApiErrorCodes.TenantContextMissing);
        }

        var record = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId && t.DeletedAt == null)
            .Select(t => new
            {
                t.Id,
                t.Name,
                Config = _db.TenantConfigs.Where(c => c.TenantId == t.Id).Select(c => new { c.Branding, c.BrandingVersion }).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (record?.Config is null)
        {
            return Result<BrandingResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.BrandingTenantNotFound);
        }

        var branding = BrandingDefaults.Parse(record.Config.Branding, record.Name);

        return Result<BrandingResponse>.Success(new BrandingResponse(
            new TenantBrandingInfoResponse(record.Id, record.Name),
            branding,
            record.Config.BrandingVersion));
    }
}
