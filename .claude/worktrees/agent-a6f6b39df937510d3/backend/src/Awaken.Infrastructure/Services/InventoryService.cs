using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Services;

public class InventoryService(
    IInventoryRepository inventoryRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IInventoryService
{
    /// US-227 / RN-006: numero maximo de tentativas para um incremento em
    /// caso de conflito de concorrencia otimista (xmin) antes de desistir e
    /// sinalizar erro controlado ao chamador.
    private const int MaxConcurrencyRetries = 3;

    public async Task<int> GetQuantityAsync(
        Guid userId, string itemKey, CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(userId, itemKey, cancellationToken);
        return item?.Quantity ?? 0;
    }

    public async Task<InventoryItem> IncrementAsync(
        Guid userId,
        string itemKey,
        int amount,
        CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(userId, itemKey, cancellationToken);
        var isNew = item is null;

        if (item is null)
        {
            // RN-005: Create com quantidade 0 e depois Add valida o sinal do
            // incremento da mesma forma que o caminho de update.
            item = InventoryItem.Create(userId, itemKey);
        }

        var maxQuantity = ItemCatalog.Find(itemKey)?.MaxQuantity ?? 99;

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            if (item.Quantity + amount > maxQuantity)
                throw new ConflictException(
                    "INVENTORY_MAX_QUANTITY",
                    $"Quantidade maxima de {maxQuantity} atingida para o item '{itemKey}'.");

            item.Add(amount, dateTimeService.UtcNow);

            if (isNew && attempt == 1)
                await inventoryRepository.AddAsync(item, cancellationToken);
            else
                inventoryRepository.Update(item);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return item;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == MaxConcurrencyRetries)
                    throw new ConcurrencyConflictException();

                // RN-006: recarrega o estado real do banco antes de recalcular
                // o incremento, evitando "lost update" em compras simultaneas.
                await inventoryRepository.ReloadAsync(item, cancellationToken);
            }
        }

        throw new ConcurrencyConflictException();
    }
}
