using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.UpdateTable;

/// <summary>Atualiza rótulo/capacidade/ambiente/ordem de uma mesa. Porta de <c>PATCH /v1/tables/{id}</c>.</summary>
public sealed record UpdateTableCommand(Guid Id, Guid AreaId, string Label, short Seats, short SortOrder) : ICommand<TableResponse>;
