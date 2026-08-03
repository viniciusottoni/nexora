using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Areas.Commands.SetAreaActive;

/// <summary>
/// Ativa/desativa um ambiente. Porta de <c>POST /v1/areas/{id}/activate</c> e
/// <c>POST /v1/areas/{id}/deactivate</c>.
/// </summary>
public sealed record SetAreaActiveCommand(Guid Id, bool Active) : ICommand;
