using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Implementação da extensão de US-155 (Proprietários, usuários iniciais e convites) de
/// <see cref="Application.Abstractions.Persistence.IApplicationDbContext"/> — ver docstring de
/// <c>IApplicationDbContext.Ownership.cs</c> para o motivo de cada porta existir aqui e não lá.
/// </summary>
public partial class AppDbContext
{
    public DbSet<Domain.Platform.OwnershipTransfer> OwnershipTransfers => Set<Domain.Platform.OwnershipTransfer>();

    public async Task<Guid?> LockOwnerRoleIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        // FOR UPDATE só tem efeito de trava enquanto a transação que a solicitou está aberta — o
        // TransactionBehavior já garante que existe uma (BeginTransactionAsync antes do handler
        // rodar, Commit/Rollback só depois dele terminar). Sem alias explícito: EFCore.NamingConventions
        // aplica snake_case a QUALQUER SqlQueryRaw<T> (mesma nota de AppDbContext.Auth.FindLoginCredentialByEmailAsync),
        // então a coluna projetada precisa se chamar "id" para casar com Guid via conversão implícita
        // de coluna única — SqlQueryRaw<Guid> espera exatamente uma coluna no resultado.
        var results = await Database
            .SqlQueryRaw<Guid>(
                """
                SELECT id FROM role WHERE tenant_id = {0} AND code = 'OWNER' FOR UPDATE
                """,
                tenantId)
            .ToListAsync(cancellationToken);

        return results.Count > 0 ? results[0] : null;
    }
}
