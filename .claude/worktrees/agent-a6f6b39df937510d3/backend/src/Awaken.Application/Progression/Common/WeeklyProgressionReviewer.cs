using System.Security.Cryptography;
using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Progression;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Progression.Common;

/// US-241: reavalia semanalmente (ou por mudança de perfil, via comparação de
/// hash) o estado do jogador e emite o `WeeklyProgressionPlan` consumido pela
/// geração (US-153/US-240). Fica em Application (orquestra repositórios),
/// mesmo precedente de <see cref="Awaken.Application.Quests.Common.DailyWorkoutBlueprintBuilder"/>.
public class WeeklyProgressionReviewer(
    IWeeklyProgressionStateRepository stateRepository,
    IQuestLogRepository questLogRepository,
    IDateTimeService dateTimeService,
    ILogger<WeeklyProgressionReviewer> logger)
{
    private const int RecentFeelingsWindowDays = 7;

    public async Task<WeeklyProgressionPlan> ReviewAsync(
        Guid userId, UserProfile profile, HunterProgression? progression, CancellationToken ct = default)
    {
        var utcNow = dateTimeService.UtcNow;
        var currentWeekAnchor = WeekAnchorFor(DateOnly.FromDateTime(utcNow));
        var profileHash = ComputeProfileHash(profile);

        var state = await stateRepository.GetByUserIdAsync(userId, ct);
        var isFirstEvaluation = state is null;
        if (state is null)
        {
            state = WeeklyProgressionState.CreateInitial(userId, currentWeekAnchor, profileHash, utcNow);
            await stateRepository.AddAsync(state, ct);
        }

        var isNewWeek = state.WeekAnchorDate != currentWeekAnchor;
        var profileChanged = !isFirstEvaluation && state.ProfileSnapshotHash != profileHash;

        if (!isFirstEvaluation && !isNewWeek && !profileChanged)
        {
            // Mesma semana, sem mudança de perfil: devolve o plano já vigente (idempotente).
            return BuildPlan(state, currentWeekAnchor, recalibrated: false);
        }

        var recentLogs = await questLogRepository.GetCompletedSinceAsync(
            userId, utcNow.AddDays(-RecentFeelingsWindowDays), ct);
        var recentFeelings = recentLogs
            .Where(l => l.PerceivedFeeling is not null)
            .Select(l => l.PerceivedFeeling!)
            .ToList();

        var attributes = progression is null
            ? new Dictionary<string, int>()
            : new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["strength"] = progression.Strength,
                ["agility"] = progression.Agility,
                ["endurance"] = progression.Endurance,
                ["vitality"] = progression.Vitality,
                ["focus"] = progression.Focus,
                ["wisdom"] = progression.Wisdom,
            };

        var decision = WeeklyProgressionDecisionEngine.Decide(new WeeklyProgressionDecisionRequest(
            RecentFeelings: recentFeelings,
            ConsecutiveEasyWeeks: state.ConsecutiveEasyWeeks,
            ConsecutiveHardWeeks: state.ConsecutiveHardWeeks,
            MesocycleWeekIndex: state.MesocycleWeekIndex,
            CurrentVolumeSetsDelta: state.VolumeSetsDelta,
            Rank: progression?.Rank ?? "E",
            Attributes: attributes,
            LastAxis: state.LastAxis));

        state.ApplyWeeklyDecision(
            currentWeekAnchor,
            // A primeira avaliação nunca avança o mesociclo (já nasce em 1, via
            // CreateInitial) - só avança em um rollover de semana de um estado já existente.
            isNewWeek: isNewWeek && !isFirstEvaluation,
            decision.Decision,
            decision.Axis,
            decision.VolumeSetsDelta,
            decision.RpeDelta,
            decision.RestSecondsDelta,
            decision.ConsecutiveEasyWeeks,
            decision.ConsecutiveHardWeeks,
            decision.DeloadWeek,
            profileHash,
            utcNow);

        if (!isFirstEvaluation)
            stateRepository.Update(state);

        logger.LogInformation(
            "weekly_progression_reviewed userId={UserId} decision={Decision} axis={Axis} deloadWeek={DeloadWeek} mesocycleWeekIndex={MesocycleWeekIndex} recalibratedFromProfileChange={ProfileChanged}",
            userId, decision.Decision, decision.Axis, decision.DeloadWeek, state.MesocycleWeekIndex, profileChanged);

        if (decision.Decision == "progress")
        {
            logger.LogInformation(
                "progression_applied userId={UserId} decision={Decision} axis={Axis}",
                userId, decision.Decision, decision.Axis);
        }

        if (decision.DeloadWeek)
        {
            logger.LogInformation("progression_deload_triggered userId={UserId}", userId);
        }

        if (profileChanged)
        {
            logger.LogInformation("progression_recalibrated_profile_change userId={UserId}", userId);
        }

        return BuildPlan(state, currentWeekAnchor, recalibrated: profileChanged);
    }

    private static WeeklyProgressionPlan BuildPlan(WeeklyProgressionState state, DateOnly weekAnchor, bool recalibrated) =>
        new(
            WeekAnchorDate: weekAnchor,
            MesocycleWeekIndex: state.MesocycleWeekIndex,
            Decision: state.LastDecision,
            Axis: state.LastAxis,
            VolumeSetsDelta: state.VolumeSetsDelta,
            RpeDelta: state.RpeDelta,
            RestSecondsDelta: state.RestSecondsDelta,
            DeloadWeek: state.DeloadDue,
            RecalibratedFromProfileChange: recalibrated);

    // US-034/US-241 RN-006: hash simples e determinístico dos campos que disparam recalibração.
    private static string ComputeProfileHash(UserProfile profile)
    {
        var equipment = profile.EquipmentAvailable ?? [];
        var raw = string.Join('|',
            profile.WeightKg, profile.AvailableMinutesPerWorkout, profile.Goal, profile.ExperienceLevel,
            string.Join(',', equipment.OrderBy(e => e, StringComparer.Ordinal)));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static DateOnly WeekAnchorFor(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7; // segunda-feira = âncora da semana
        return date.AddDays(-diff);
    }
}

/// US-241 §18: plano consumido por US-153 (prescrição) e US-240 (blueprint).
public sealed record WeeklyProgressionPlan(
    DateOnly WeekAnchorDate,
    int MesocycleWeekIndex,
    string Decision,
    string? Axis,
    int VolumeSetsDelta,
    int RpeDelta,
    int RestSecondsDelta,
    bool DeloadWeek,
    bool RecalibratedFromProfileChange);
