using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Areas.Commands.UpdateArea;

/// <summary>Renomeia/reposiciona um ambiente do salão. Porta de <c>PATCH /v1/areas/{id}</c>.</summary>
public sealed record UpdateAreaCommand(Guid Id, string Name, short Position) : ICommand<AreaResponse>;
