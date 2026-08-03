using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.CreateTablesBulk;

/// <summary>
/// Cria mesas com rótulos sequenciais em lote (US-020, cenário Gherkin "Criação em lote": "criar
/// mesas 1 a 20"). Porta de <c>POST /v1/tables/bulk</c>. Transacional por natureza — todas as
/// mesas do lote são um único <c>ICommand</c>, então <c>TransactionBehavior</c> as grava (ou
/// nenhuma, se qualquer regra falhar) num único <c>SaveChangesAsync</c>.
/// </summary>
public sealed record CreateTablesBulkCommand(Guid AreaId, int From, int To, short Seats) : ICommand<TablesBulkResponse>;
