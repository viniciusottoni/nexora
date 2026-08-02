using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Associação usuário↔papel, opcionalmente restrita a uma loja (store_id nulo = todas as lojas).
/// </summary>
public sealed class UserRole
{
    private UserRole() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? StoreId { get; private set; }

    public AppUser User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    public static UserRole Create(Guid tenantId, Guid userId, Guid roleId, Guid? storeId = null)
    {
        return new UserRole
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            StoreId = storeId
        };
    }
}
