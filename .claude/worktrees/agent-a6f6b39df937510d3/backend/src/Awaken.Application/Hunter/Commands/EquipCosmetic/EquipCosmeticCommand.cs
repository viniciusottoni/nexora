using MediatR;

namespace Awaken.Application.Hunter.Commands.EquipCosmetic;

/// US-230: equipa (ou remove, se ItemKey for null) um cosmético de moldura/aura/fundo
/// no slot informado.
public record EquipCosmeticCommand(string Slot, string? ItemKey) : IRequest<Unit>;
