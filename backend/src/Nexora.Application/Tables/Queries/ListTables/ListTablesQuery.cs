using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Queries.ListTables;

/// <summary>Lista as mesas do tenant autenticado, opcionalmente filtradas por ambiente. Porta de <c>GET /v1/tables?areaId=...</c>.</summary>
public sealed record ListTablesQuery(Guid? AreaId) : IQuery<TableListResponse>;
