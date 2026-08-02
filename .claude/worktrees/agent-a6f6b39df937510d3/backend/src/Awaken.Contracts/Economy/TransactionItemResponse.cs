namespace Awaken.Contracts.Economy;

/// <summary>
/// US-192: item de extrato combinando movimentações de Gold e pedidos de compra.
/// </summary>
/// <param name="Id">Identificador do registro original.</param>
/// <param name="Type">"gold_movement" ou "shop_order".</param>
/// <param name="Description">Motivo ou descricao legivel do lancamento.</param>
/// <param name="Direction">"credit" | "debit" | null (para shop_order).</param>
/// <param name="Amount">Quantidade de Gold movimentada (null para shop_order).</param>
/// <param name="BalanceAfter">Saldo apos o lancamento (null para shop_order).</param>
/// <param name="Channel">Canal de venda: "gold" | "iap" (null para gold_movement).</param>
/// <param name="ProductKey">Chave do produto comprado (null para gold_movement).</param>
/// <param name="Status">Status do pedido: "pending" | "granted" | "failed" | "refunded" (null para gold_movement).</param>
/// <param name="CreatedAtUtc">Data de criacao em UTC; o frontend converte para fuso local.</param>
public record TransactionItemResponse(
    Guid Id,
    string Type,
    string Description,
    string? Direction,
    long? Amount,
    long? BalanceAfter,
    string? Channel,
    string? ProductKey,
    string? Status,
    DateTime CreatedAtUtc);
