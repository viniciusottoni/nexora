using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Areas.Commands.DeleteArea;

/// <summary>Exclui (soft delete) um ambiente sem mesas. Porta de <c>DELETE /v1/areas/{id}</c>.</summary>
public sealed record DeleteAreaCommand(Guid Id) : ICommand;
