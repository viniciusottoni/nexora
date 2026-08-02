namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-227: abstrai uma transação de banco explícita que pode abranger
/// múltiplos SaveChangesAsync (ex.: débito de Gold + ledger + inventário +
/// status do pedido), garantindo que tudo seja confirmado ou revertido em
/// conjunto (RN-001/RN-002).
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
