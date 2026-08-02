using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Awaken.Infrastructure.ItemEffects;

// US-230: handlers de efeito de consumíveis.

public class SubstitutionScrollEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.SubstitutionScroll;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 2;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        if (context.ContextId is not { } questIdRaw || !Guid.TryParse(questIdRaw, out var questId))
            throw new ConflictException(
                "SUBSTITUTION_CONTEXT_INVALID", "Contexto inválido para substituição de exercício.");

        var payload = ItemEffectPayload.Parse<SubstitutionPayload>(context.PayloadJson);
        if (payload?.QuestExerciseId is not { } questExerciseIdRaw || !Guid.TryParse(questExerciseIdRaw, out var questExerciseId))
            throw new ConflictException(
                "SUBSTITUTION_CONTEXT_INVALID", "Contexto inválido para substituição de exercício.");

        var questRepository = context.Services.GetRequiredService<IQuestRepository>();
        var quest = await questRepository.GetByIdWithExercisesAsync(questId, ct)
            ?? throw new NotFoundException("Quest", questId);

        if (quest.UserId != context.UserId)
            throw new NotFoundException("Quest", questId);

        var exercise = quest.Exercises.FirstOrDefault(e => e.Id == questExerciseId)
            ?? throw new NotFoundException("QuestExercise", questExerciseId);

        if (exercise.Status != "pending")
            throw new ConflictException(
                "SUBSTITUTION_NOT_ALLOWED", "Só é possível substituir um exercício pendente.");

        // Reaproveita o snapshot de perfil da geração original (mesmo
        // perfil/equipamento/segurança), ou reconstrói se indisponível.
        var fitnessProfileJson = quest.ProfileSnapshotJson;
        if (string.IsNullOrWhiteSpace(fitnessProfileJson))
        {
            var userProfileRepository = context.Services.GetRequiredService<IUserProfileRepository>();
            var hunterProgressionRepository = context.Services.GetRequiredService<IHunterProgressionRepository>();
            var profile = await userProfileRepository.GetByUserIdAsync(context.UserId, ct)
                ?? throw new NotFoundException("UserProfile", context.UserId);
            var progression = await hunterProgressionRepository.GetByUserIdAsync(context.UserId, ct);
            fitnessProfileJson = FitnessProfileSnapshot.Build(profile, progression);
        }

        var excludeIds = quest.Exercises
            .Where(e => e.ExerciseCatalogProviderId is not null)
            .Select(e => e.ExerciseCatalogProviderId!)
            .ToList();

        var workoutGeneratorService = context.Services.GetRequiredService<IWorkoutGeneratorService>();
        var substituteSeed = await workoutGeneratorService.SelectSubstituteExerciseAsync(
            fitnessProfileJson, excludeIds, ct);

        if (substituteSeed is null)
            throw new ConflictException(
                "SUBSTITUTION_NO_CANDIDATE", "Nenhum exercício compatível disponível para substituição.");

        exercise.ReplaceWith(substituteSeed, context.UtcNow);
        questRepository.Update(quest);

        return new ItemEffectResult(true, "exercise_substitution_applied");
    }

    private record SubstitutionPayload(string? QuestExerciseId);
}

public class DungeonCompassEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.DungeonCompass;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
        => Task.FromResult(new ItemEffectResult(true, "dungeon_compass_granted"));
}

public class DungeonKeyEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.DungeonKey;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
        => Task.FromResult(new ItemEffectResult(true, "dungeon_key_granted"));
}

public class ProtectionSealEffectHandler : IItemEffectHandler
{
    private const int MaxActiveEffects = 2;

    public string ItemKey => ItemKeys.ProtectionSeal;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 0; // limite "max 2 ativos" é aplicado abaixo, não por período.
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Unlimited;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var effectRepository = context.Services.GetRequiredService<IItemActiveEffectRepository>();
        var activeEffects = await effectRepository.GetActiveByUserAndTypeAsync(
            context.UserId, ItemEffectTypes.StreakProtection, ct);
        if (activeEffects.Count >= MaxActiveEffects)
            throw new ConflictException(
                "PROTECTION_SEAL_MAX_ACTIVE", "Você já tem 2 Selos de Proteção ativos.");

        var effect = ItemActiveEffect.Create(
            context.UserId,
            context.ItemKey,
            ItemEffectTypes.StreakProtection,
            activatedAtUtc: context.UtcNow,
            expiresAtUtc: context.EffectiveQuestDateUtc.AddDays(30));

        await effectRepository.AddAsync(effect, ct);

        return new ItemEffectResult(true, "streak_protection_applied");
    }
}

public class RecoveryTonicEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.RecoveryTonic;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 2;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Weekly;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var effectRepository = context.Services.GetRequiredService<IItemActiveEffectRepository>();

        // EffectDateUtc = dia local do usuário (P0-2/P0-3): marca ESTE dia como
        // recuperação ativa, consultado por questDateUtc (não por UtcNow de
        // execução) quando o rollout de penalidade processar esse dia.
        var effect = ItemActiveEffect.Create(
            context.UserId,
            context.ItemKey,
            ItemEffectTypes.RecoveryDay,
            activatedAtUtc: context.UtcNow,
            expiresAtUtc: context.EffectiveQuestDateUtc.AddDays(1),
            effectDateUtc: context.EffectiveQuestDateUtc);

        await effectRepository.AddAsync(effect, ct);

        return new ItemEffectResult(true, "recovery_day_granted");
    }
}

public class ReturnAmuletEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.ReturnAmulet;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Weekly;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var progressionRepository = context.Services.GetRequiredService<IHunterProgressionRepository>();
        var progression = await progressionRepository.GetByUserIdAsync(context.UserId, ct);

        var yesterday = context.EffectiveQuestDateUtc.AddDays(-1);
        var eligible = progression is not null
            && progression.RecentLostStreakDays is > 0
            && progression.RecentDailyPenaltyQuestDateUtc?.Date == yesterday.Date;

        if (!eligible)
            throw new ConflictException(
                "RETURN_AMULET_NOT_ELIGIBLE", "Nenhum streak perdido ontem para recuperar.");

        var effectRepository = context.Services.GetRequiredService<IItemActiveEffectRepository>();
        var effect = ItemActiveEffect.Create(
            context.UserId,
            context.ItemKey,
            ItemEffectTypes.StreakRecovery,
            activatedAtUtc: context.UtcNow,
            expiresAtUtc: context.EffectiveQuestDateUtc.AddDays(1),
            streakDaysToRestore: progression!.RecentLostStreakDays,
            effectDateUtc: context.EffectiveQuestDateUtc);

        await effectRepository.AddAsync(effect, ct);

        return new ItemEffectResult(true, "streak_recovery_granted");
    }
}

public class FocusPotionEffectHandler : IItemEffectHandler
{
    private const decimal Multiplier = 0.25m;

    public string ItemKey => ItemKeys.FocusPotion;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var effectRepository = context.Services.GetRequiredService<IItemActiveEffectRepository>();

        // US-230/P0-1: +25% XP no próximo TREINO concluído (não exercício
        // isolado) — consumido em CompleteQuestCommandHandler sobre o total.
        var effect = ItemActiveEffect.Create(
            context.UserId,
            context.ItemKey,
            ItemEffectTypes.XpBoost,
            activatedAtUtc: context.UtcNow,
            expiresAtUtc: context.EffectiveQuestDateUtc.AddDays(1),
            xpBoostMultiplier: Multiplier);

        await effectRepository.AddAsync(effect, ct);

        return new ItemEffectResult(true, "xp_boost_25_applied");
    }
}

public class FocusPotionLargeEffectHandler : IItemEffectHandler
{
    private const decimal Multiplier = 0.50m;

    public string ItemKey => ItemKeys.FocusPotionLarge;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Weekly;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var effectRepository = context.Services.GetRequiredService<IItemActiveEffectRepository>();

        var effect = ItemActiveEffect.Create(
            context.UserId,
            context.ItemKey,
            ItemEffectTypes.XpBoost,
            activatedAtUtc: context.UtcNow,
            expiresAtUtc: context.EffectiveQuestDateUtc.AddDays(1),
            xpBoostMultiplier: Multiplier);

        await effectRepository.AddAsync(effect, ct);

        return new ItemEffectResult(true, "xp_boost_50_applied");
    }
}

public class LuckPotionEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.LuckPotion;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
        => Task.FromResult(new ItemEffectResult(true, "luck_boost_applied"));
}

public class DungeonStoneEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.DungeonStone;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
        => Task.FromResult(new ItemEffectResult(true, "dungeon_stone_used"));
}

public class ScrollRenameEffectHandler : IItemEffectHandler
{
    private const int MinLength = 2;
    private const int MaxLength = 40;

    public string ItemKey => ItemKeys.ScrollRename;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 0;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Unlimited;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var payload = ItemEffectPayload.Parse<RenamePayload>(context.PayloadJson);
        var newDisplayName = payload?.NewDisplayName?.Trim();

        if (string.IsNullOrEmpty(newDisplayName) || newDisplayName.Length is < MinLength or > MaxLength)
            throw new ConflictException(
                "INVALID_DISPLAY_NAME", $"Nome deve ter entre {MinLength} e {MaxLength} caracteres.");

        var userRepository = context.Services.GetRequiredService<IUserRepository>();
        var user = await userRepository.GetByIdAsync(context.UserId, ct)
            ?? throw new NotFoundException("User", context.UserId);

        // ADR-015: nunca logar/auditar o novo nome (dado pessoal) — só o marcador.
        user.Rename(newDisplayName, context.UtcNow);
        userRepository.Update(user);

        return new ItemEffectResult(true, "rename_granted");
    }

    private record RenamePayload(string? NewDisplayName);
}

public class ScrollClassChangeEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.ScrollClassChange;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 0;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Unlimited;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var payload = ItemEffectPayload.Parse<ClassChangePayload>(context.PayloadJson);
        var targetClass = payload?.TargetClass;

        if (string.IsNullOrWhiteSpace(targetClass))
            throw new ConflictException("HUNTER_CLASS_NOT_FOUND", "Classe inválida.");

        var classEntry = HunterClassCatalog.Find(targetClass)
            ?? throw new ConflictException("HUNTER_CLASS_NOT_FOUND", "Classe inválida.");

        if (classEntry.RequiredItemKey is not null)
        {
            var inventoryRepository = context.Services.GetRequiredService<IInventoryRepository>();
            var pack = await inventoryRepository.GetByUserIdAndItemKeyAsync(
                context.UserId, classEntry.RequiredItemKey, ct);

            if (pack is null || pack.Quantity <= 0)
                throw new ConflictException(
                    "HUNTER_CLASS_LOCKED", "Esta classe exige um pack que você ainda não possui.");
        }

        var progressionRepository = context.Services.GetRequiredService<IHunterProgressionRepository>();
        var progression = await progressionRepository.GetByUserIdAsync(context.UserId, ct)
            ?? throw new NotFoundException("HunterProgression", context.UserId);

        progression.ChangeClass(classEntry.ClassKey, context.UtcNow);

        return new ItemEffectResult(true, "class_change_granted");
    }

    private record ClassChangePayload(string? TargetClass);
}
