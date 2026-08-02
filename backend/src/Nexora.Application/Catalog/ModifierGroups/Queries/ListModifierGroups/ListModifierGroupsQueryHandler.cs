using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.ModifierGroups.Queries.ListModifierGroups;

internal sealed class ListModifierGroupsQueryHandler : IRequestHandler<ListModifierGroupsQuery, Result<ModifierGroupListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListModifierGroupsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ModifierGroupListResponse>> Handle(ListModifierGroupsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ModifierGroupListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:read"))
        {
            return Result<ModifierGroupListResponse>.Failure(
                "Seu perfil não tem permissão para consultar o cardápio.",
                ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;

        // Três consultas simples em vez de Include duplo (evita explosão cartesiana de duas
        // coleções filhas na mesma query) — o volume de grupos/modificadores por tenant é pequeno
        // (dezenas, não milhares), então compor em memória é preferível a split query.
        var groups = await _db.ModifierGroups
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.DeletedAt == null)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        var groupIds = groups.Select(g => g.Id).ToList();

        var modifiersByGroup = await _db.Modifiers
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.DeletedAt == null && groupIds.Contains(m.GroupId))
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .Select(m => new ModifierResponse(m.Id, m.GroupId, m.Name, m.PriceDelta, m.IngredientId, m.Quantity, m.IsAvailable, m.SortOrder))
            .ToListAsync(cancellationToken);

        var productIdsByGroup = await _db.ProductModifierGroups
            .AsNoTracking()
            .Where(pg => pg.TenantId == tenantId && groupIds.Contains(pg.GroupId))
            .Select(pg => new { pg.GroupId, pg.ProductId })
            .ToListAsync(cancellationToken);

        var modifierLookup = modifiersByGroup.GroupBy(m => m.GroupId).ToDictionary(g => g.Key, g => (IReadOnlyList<ModifierResponse>)g.ToList());
        var productLookup = productIdsByGroup.GroupBy(pg => pg.GroupId).ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(pg => pg.ProductId).ToList());

        var items = groups
            .Select(g => new ModifierGroupResponse(
                g.Id,
                g.Name,
                g.MinSelect,
                g.MaxSelect,
                g.IsRequired,
                g.SortOrder,
                modifierLookup.TryGetValue(g.Id, out var mods) ? mods : Array.Empty<ModifierResponse>(),
                productLookup.TryGetValue(g.Id, out var products) ? products : Array.Empty<Guid>()))
            .ToList();

        return Result<ModifierGroupListResponse>.Success(new ModifierGroupListResponse(items));
    }
}
