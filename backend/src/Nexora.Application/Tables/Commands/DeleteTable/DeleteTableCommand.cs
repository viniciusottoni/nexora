using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tables.Commands.DeleteTable;

/// <summary>Exclui (soft delete) uma mesa sem histórico. Porta de <c>DELETE /v1/tables/{id}</c>.</summary>
public sealed record DeleteTableCommand(Guid TableId) : ICommand;
