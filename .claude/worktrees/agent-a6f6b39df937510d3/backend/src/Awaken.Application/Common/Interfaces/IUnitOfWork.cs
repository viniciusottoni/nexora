namespace Awaken.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// US-227: abre uma transação explícita de banco que pode abranger
    /// múltiplos SaveChangesAsync subsequentes, garantindo atomicidade real
    /// entre débito de Gold, ledger, inventário e status do pedido
    /// (RN-001/RN-002).
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// US-227: limpa tracking pendente após rollback para evitar que mudanças
    /// parcialmente aplicadas sejam regravadas por um SaveChanges subsequente.
    /// </summary>
    void ClearChangeTracker();
}
