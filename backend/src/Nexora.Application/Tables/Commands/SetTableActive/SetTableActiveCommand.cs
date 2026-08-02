using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tables.Commands.SetTableActive;

/// <summary>
/// Ativa/desativa uma mesa. Porta de <c>POST /v1/tables/{id}/activate</c> e
/// <c>POST /v1/tables/{id}/deactivate</c> — a via de "desativação em vez de exclusão" oferecida
/// pelo cenário Gherkin "Exclusão de mesa com histórico".
/// </summary>
public sealed record SetTableActiveCommand(Guid TableId, bool Active) : ICommand;
