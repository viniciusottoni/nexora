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

    /// <summary>
    /// US-155 · Proprietários, usuários iniciais e convites — repõe o usuário desta atribuição de
    /// papel (usado por <c>TransferTenantOwnershipCommandHandler</c> para transferir o papel OWNER:
    /// a MESMA linha de <see cref="UserRole"/> passa a apontar para o novo dono, em vez de excluir a
    /// linha antiga e inserir uma nova). Escolhido de propósito em vez de excluir+inserir: o papel de
    /// runtime da aplicação (<c>app_user_role</c>) só tem <c>GRANT SELECT, INSERT, UPDATE</c> nas
    /// tabelas de negócio (migration <c>EnableRowLevelSecurity</c>) — nunca <c>DELETE</c>, reflexo do
    /// princípio "DELETE físico não existe" deste projeto (soft delete sempre, quando a entidade tem
    /// <c>deleted_at</c>; quando não tem, como aqui, a operação é UPDATE em vez de DELETE, nunca um
    /// DELETE físico disfarçado). Repointing preserva o Id/StoreId da atribuição original.
    /// </summary>
    public void TransferTo(Guid newUserId)
    {
        if (newUserId == Guid.Empty)
            throw new DomainException("O novo usuário da atribuição é obrigatório.");

        UserId = newUserId;
    }
}
