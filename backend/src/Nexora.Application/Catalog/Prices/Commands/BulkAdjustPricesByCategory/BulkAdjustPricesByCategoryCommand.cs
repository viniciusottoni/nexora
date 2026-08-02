using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;

/// <summary>
/// Reajuste percentual em massa (US-014 §7, cenário Gherkin "Reajuste em massa") — aplica
/// <see cref="Percent"/> sobre o preço efetivo (próprio ou herdado de <c>DineIn</c>) de
/// <see cref="Channel"/>, para todas as variações ativas dos produtos ativos de
/// <see cref="CategoryId"/>. Transacional: todas as linhas de <c>Price</c> criadas e o
/// <c>AuditLog</c> entram na mesma <c>SaveChangesAsync</c> (feita pelo <c>TransactionBehavior</c>);
/// se qualquer variação resultaria em preço negativo, a chamada inteira é recusada antes de
/// qualquer mutação (nenhuma entidade é adicionada ao <c>DbContext</c>). Porta de
/// <c>POST /v1/catalog/prices/bulk-adjust</c>.
/// </summary>
public sealed record BulkAdjustPricesByCategoryCommand(Guid CategoryId, string Channel, decimal Percent)
    : ICommand<BulkAdjustPricesResponse>;
