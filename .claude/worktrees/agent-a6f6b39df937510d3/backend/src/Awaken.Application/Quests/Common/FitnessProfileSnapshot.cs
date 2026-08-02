using System.Text.Json;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;

namespace Awaken.Application.Quests.Common;

/// Snapshot serializado do perfil considerado na geracao/regeneracao da quest
/// (US-049 RN-002): usado tanto para alimentar o gerador de treino quanto para
/// auditoria, sem dados alem do necessario (ADR-014).
/// US-151/152: inclui atributos do Hunter para targetAttributeScore.
public static class FitnessProfileSnapshot
{
    public static string Build(UserProfile profile, HunterProgression? progression = null) =>
        JsonSerializer.Serialize(new
        {
            goal = profile.Goal,
            experienceLevel = profile.ExperienceLevel,
            trainingDuration = profile.TrainingDuration,
            effectiveExperienceLevel = profile.EffectiveExperienceLevel,
            availableMinutesPerWorkout = profile.AvailableMinutesPerWorkout,
            equipmentAvailable = profile.EquipmentAvailable,
            bodyType = profile.BodyType,
            physicalLimitations = profile.PhysicalLimitations,
            physicalPains = profile.PhysicalPains,
            heightCm = profile.HeightCm,
            weightKg = profile.WeightKg,
            userAttributes = progression is null ? null : new
            {
                strength = progression.Strength,
                endurance = progression.Endurance,
                agility = progression.Agility,
                vitality = progression.Vitality,
                focus = progression.Focus,
                wisdom = progression.Wisdom,
            },
        });
}
