using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Modifiers.Commands.MarkModifierAvailable;

/// <summary>Marca um modificador como disponível de novo. Porta de <c>PATCH .../modifiers/{modifierId}/availability</c> com <c>isAvailable=true</c> (US-012).</summary>
public sealed record MarkModifierAvailableCommand(Guid GroupId, Guid ModifierId) : ICommand<ModifierResponse>;
