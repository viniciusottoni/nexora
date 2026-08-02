using Awaken.Domain.Entities.Inventory;

namespace Awaken.Application.Common.Interfaces;

/// US-187: operacoes genericas de inventario, reutilizaveis por qualquer
/// fluxo que precise consultar ou conceder itens por ItemKey (ex.: concessao
/// de compra na US-189), sem acoplar a logica de negocio especifica de cada
/// item. Nao expoe consumo generico (RN-004) - isso permanece em fluxos
/// especificos (ex.: RegenerateDailyQuestCommandHandler).
public interface IInventoryService
{
    /// Retorna a quantidade do item no inventario do usuario. Item nunca
    /// obtido retorna 0 sem erro (CA-001).
    Task<int> GetQuantityAsync(Guid userId, string itemKey, CancellationToken cancellationToken = default);

    /// Incrementa a quantidade do item no inventario do usuario, criando o
    /// registro se for o primeiro item desta chave. RN-005: a quantidade
    /// resultante nunca pode ficar negativa.
    Task<InventoryItem> IncrementAsync(
        Guid userId,
        string itemKey,
        int amount,
        CancellationToken cancellationToken = default);
}
