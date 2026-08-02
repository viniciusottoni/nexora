using Awaken.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Awaken.Infrastructure.Persistence;

/// <summary>
/// US-227: adapta o IDbContextTransaction do EF Core para a abstração
/// IUnitOfWorkTransaction usada pela camada de aplicação, mantendo a
/// Application livre de dependência direta do EF Core.
/// </summary>
public class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
