using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>
/// US-230 RN-005: implementação EF Core do repositório de efeitos ativos de item.
/// </summary>
public class ItemActiveEffectRepository(AwakenDbContext context) : IItemActiveEffectRepository
{
    public async Task<List<ItemActiveEffect>> GetActiveByUserAndTypeAsync(
        Guid userId,
        string effectType,
        CancellationToken cancellationToken = default)
        => await context.ItemActiveEffects
            .Where(e => e.UserId == userId && e.EffectType == effectType && e.Status == "active")
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ItemActiveEffect effect, CancellationToken cancellationToken = default)
        => await context.ItemActiveEffects.AddAsync(effect, cancellationToken);

    public void Update(ItemActiveEffect effect) => context.ItemActiveEffects.Update(effect);
}
