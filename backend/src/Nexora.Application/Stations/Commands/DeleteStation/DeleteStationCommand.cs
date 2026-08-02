using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Stations.Commands.DeleteStation;

/// <summary>
/// Exclui (soft delete) uma praça de produção do tenant autenticado. Porta de
/// <c>DELETE /v1/catalog/stations/:id</c>. Recusado quando existem produtos vinculados
/// (US-017 §4, cenário "Exclusão de praça com produtos vinculados").
/// </summary>
public sealed record DeleteStationCommand(Guid StationId) : ICommand;
