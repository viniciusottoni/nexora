using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Roles;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Roles.Queries.ListRoles;

internal sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, Result<RoleListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListRolesQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<RoleListResponse>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<RoleListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.DeletedAt == null)
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.Permissions,
                r.IsSystem,
                UserCount = _db.UserRoles.Count(ur => ur.RoleId == r.Id)
            })
            .ToListAsync(cancellationToken);

        var allowed = new HashSet<string>(PermissionCatalog.AllCodes, StringComparer.Ordinal);

        var items = roles
            .Select(r =>
            {
                var permissions = (JsonSerializer.Deserialize<string[]>(r.Permissions) ?? Array.Empty<string>())
                    .Where(allowed.Contains)
                    .ToList();

                return new RoleResponse(r.Id, r.Code, r.Name, permissions, r.IsSystem, r.UserCount);
            })
            .ToList();

        var catalog = PermissionCatalog.Build(PermissionCatalog.AllCodes)
            .Select(entry => new PermissionCatalogItemResponse(entry.Code, entry.Resource, entry.Description, entry.Sensitive))
            .ToList();

        return Result<RoleListResponse>.Success(new RoleListResponse(items, catalog));
    }
}
