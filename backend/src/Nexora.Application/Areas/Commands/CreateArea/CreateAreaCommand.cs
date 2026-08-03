using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Areas.Commands.CreateArea;

/// <summary>Cadastra um ambiente do salão (US-020). Porta de <c>POST /v1/areas</c>.</summary>
public sealed record CreateAreaCommand(string Name, short Position) : ICommand<AreaResponse>;
