using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.TenantDomains.Queries.ListTenantDomains;

/// <summary>
/// Leitura de plataforma sobre <c>tenant_domain</c> — tabela COM RLS (ao contrário de <c>tenant</c>,
/// Docs/Domain/10 §1), então a visão "todos os tenants" (<c>TenantId</c> nulo) precisa do mesmo
/// padrão de <c>ListPlatformInstallationsQueryHandler</c>: iterar <c>tenant</c> (sem RLS) fixando
/// <c>app.tenant_id</c> por vez antes de cada leitura escopada.
/// </summary>
internal sealed class ListTenantDomainsQueryHandler : IRequestHandler<ListTenantDomainsQuery, Result<TenantDomainListResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListTenantDomainsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantDomainListResponse>> Handle(ListTenantDomainsQuery request, CancellationToken cancellationToken)
    {
        if (request.TenantId is { } tenantId)
        {
            await _db.SetTenantContextAsync(tenantId, cancellationToken);

            var scoped = await _db.TenantDomains.AsNoTracking()
                .Where(d => d.TenantId == tenantId && d.DeletedAt == null)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<TenantDomainListResponse>.Success(
                new TenantDomainListResponse(scoped.Select(d => d.ToResponse()).ToList()));
        }

        var tenantIds = await _db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var all = new List<TenantDomainResponse>();
        foreach (var id in tenantIds)
        {
            await _db.SetTenantContextAsync(id, cancellationToken);

            var domains = await _db.TenantDomains.AsNoTracking()
                .Where(d => d.TenantId == id && d.DeletedAt == null)
                .ToListAsync(cancellationToken);

            all.AddRange(domains.Select(d => d.ToResponse()));
        }

        return Result<TenantDomainListResponse>.Success(
            new TenantDomainListResponse(all.OrderByDescending(d => d.CreatedAt).ToList()));
    }
}
