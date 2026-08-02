using Awaken.Domain.Common;
using Awaken.Domain.Entities.Economy;

namespace Awaken.Domain.Repositories;

public interface IGoldWalletRepository : IRepository<GoldWallet>
{
    Task<GoldWallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// US-227 / RN-005: recarrega o estado da entidade rastreada a partir do
    /// banco (incluindo Version/xmin), necessário após um conflito de
    /// concorrência otimista para que o retry opere sobre o saldo atual.
    /// </summary>
    Task ReloadAsync(GoldWallet wallet, CancellationToken cancellationToken = default);
}
