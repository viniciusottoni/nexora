using Awaken.Contracts.Economy;
using MediatR;

namespace Awaken.Application.Economy.Queries.GetTransactions;

/// <summary>
/// US-192: consulta paginada do extrato de transacoes do usuario autenticado,
/// combinando movimentacoes de Gold (GoldLedgerEntry) e pedidos de compra (ShopOrder),
/// ordenados por data decrescente.
/// </summary>
public record GetTransactionsQuery(int Page = 1, int PageSize = 20) : IRequest<TransactionPageResponse>;
