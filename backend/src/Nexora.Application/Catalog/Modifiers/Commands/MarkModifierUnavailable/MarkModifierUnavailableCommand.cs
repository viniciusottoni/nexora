using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Modifiers.Commands.MarkModifierUnavailable;

/// <summary>Marca um modificador como indisponível (ex.: insumo em falta). Porta de <c>PATCH .../modifiers/{modifierId}/availability</c> com <c>isAvailable=false</c> (US-012).</summary>
public sealed record MarkModifierUnavailableCommand(Guid GroupId, Guid ModifierId) : ICommand<ModifierResponse>;
