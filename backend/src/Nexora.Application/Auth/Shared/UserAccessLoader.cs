using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Resolve a loja efetiva de um usuário autenticado (cloud) — porta da lógica repetida em
/// findByEmail/findRefreshSession (prisma-password-auth.repository.ts): usa a loja do primeiro
/// papel com <c>store_id</c> definido; se nenhum papel for restrito a uma loja, cai para a loja
/// padrão (<c>is_default</c>) mais antiga do tenant.
/// </summary>
internal static class UserAccessLoader
{
    public static async Task<Guid?> ResolveStoreAsync(IApplicationDbContext db, AppUser user, CancellationToken cancellationToken)
    {
        var storeId = user.UserRoles.Select(userRole => userRole.StoreId).FirstOrDefault(id => id.HasValue);
        if (storeId.HasValue)
        {
            return storeId;
        }

        return await db.Stores
            .AsNoTracking()
            .Where(store => store.TenantId == user.TenantId && store.IsActive && store.DeletedAt == null)
            .OrderByDescending(store => store.IsDefault)
            .ThenBy(store => store.CreatedAt)
            .Select(store => (Guid?)store.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
