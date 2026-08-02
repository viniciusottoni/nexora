namespace Awaken.Contracts.Progression;

/// US-130/US-134: XP interno (0–9) de cada atributo, retornado na resposta do perfil.
public record AttributeXpDto(
    int Strength,
    int Agility,
    int Endurance,
    int Vitality,
    int Focus,
    int Wisdom);
