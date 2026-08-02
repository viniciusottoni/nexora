using MediatR;

namespace Awaken.Application.Admin.Economy.Commands.ReconcileGoldEconomy;

/// <summary>
/// US-228: dispara a reconciliação em lote entre GoldWallet, GoldLedgerEntry, ShopOrder e
/// InventoryItem, criando um SecurityAlert (via infraestrutura US-165/US-219) para cada
/// divergência detectada (RN-005). Read-only sobre a economia Gold: nunca altera saldo,
/// ledger, pedidos nem inventário — apenas lê e, quando necessário, grava SecurityAlert.
/// </summary>
public record ReconcileGoldEconomyCommand : IRequest<ReconciliationSummary>;

/// <summary>
/// Resultado da execução da reconciliação: quantos alertas novos foram criados por tipo,
/// e quantas divergências eram duplicadas (já existia um alerta aberto recente equivalente
/// e por isso não foram recriadas).
/// </summary>
public record ReconciliationSummary(
    int WalletsChecked,
    int AlertsCreated,
    int AlertsSkippedAsDuplicate,
    IReadOnlyDictionary<string, int> AlertsCreatedByType);
