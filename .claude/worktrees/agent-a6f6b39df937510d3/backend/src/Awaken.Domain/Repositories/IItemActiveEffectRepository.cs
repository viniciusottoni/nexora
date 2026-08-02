using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Repositories;

/// <summary>
/// US-230 RN-005: repositório de efeitos de item com validade (buffs pendentes).
/// </summary>
public interface IItemActiveEffectRepository
{
    /// Efeitos ativos do usuário para um EffectType, rastreados pelo change
    /// tracker (sem AsNoTracking) — necessário para que lotes que consomem e
    /// checam múltiplos efeitos no mesmo SaveChanges vejam o estado em memória,
    /// não uma leitura crua do banco.
    Task<List<ItemActiveEffect>> GetActiveByUserAndTypeAsync(
        Guid userId,
        string effectType,
        CancellationToken cancellationToken = default);

    Task AddAsync(ItemActiveEffect effect, CancellationToken cancellationToken = default);

    void Update(ItemActiveEffect effect);
}
