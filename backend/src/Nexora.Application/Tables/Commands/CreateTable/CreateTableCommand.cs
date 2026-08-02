using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.CreateTable;

/// <summary>Cadastra uma mesa física do salão (US-020). Porta de <c>POST /v1/tables</c>.</summary>
public sealed record CreateTableCommand(Guid AreaId, string Label, short Seats) : ICommand<TableResponse>;
