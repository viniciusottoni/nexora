using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Progression.Services;

/// US-070/US-129/US-132: aplica penalidade de daily perdida de forma idempotente.
public class DailyQuestPenaltyService(
    IQuestRepository questRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IItemActiveEffectRepository itemActiveEffectRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<DailyQuestPenaltyService> logger) : IDailyQuestPenaltyService
{
    public async Task<DailyPenaltyRolloverSummary> ApplyForQuestDateAsync(
        DateTime questDateUtc,
        CancellationToken cancellationToken = default)
    {
        // US-207: itera em batches de 500 para evitar carregar toda a tabela em memoria.
        const int PageSize = 500;
        Guid? lastId = null;
        var totalDailies = 0;
        var totalPenalties = 0;

        while (true)
        {
            var dailies = await questRepository
                .GetUncheckedDailiesPageAsync(questDateUtc, lastId, PageSize, cancellationToken)
                ?? [];

            if (dailies.Count == 0) break;

            var result = await ApplyToDailiesAsync(dailies, questDateUtc, cancellationToken);
            totalDailies += result.MissedDailiesChecked;
            totalPenalties += result.PenaltiesApplied;

            lastId = dailies[^1].Id;

            logger.LogInformation(
                "daily_penalty_batch processed={Count} cursor={LastId}",
                dailies.Count, lastId);

            if (dailies.Count < PageSize) break;
        }

        return new DailyPenaltyRolloverSummary(totalDailies, totalPenalties);
    }

    public async Task<DailyPenaltyRolloverSummary> ApplyForUserBeforeDateAsync(
        Guid userId,
        DateTime beforeQuestDateUtc,
        CancellationToken cancellationToken = default)
    {
        var dailies = await questRepository.GetUncheckedDailiesBeforeDateForUserAsync(
            userId,
            beforeQuestDateUtc,
            cancellationToken);

        return await ApplyToDailiesAsync(dailies, beforeQuestDateUtc.AddDays(-1), cancellationToken);
    }

    private async Task<DailyPenaltyRolloverSummary> ApplyToDailiesAsync(
        IReadOnlyList<Quest> dailies,
        DateTime fallbackQuestDateUtc,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var totalDailies = 0;
        var totalPenalties = 0;

        foreach (var quest in dailies)
        {
            totalDailies++;
            var questDateUtc = quest.QuestDateUtc == default ? fallbackQuestDateUtc : quest.QuestDateUtc;

            if (quest.Status != "completed")
            {
                totalPenalties += await TryApplyPenaltyAsync(
                    quest.UserId,
                    utcNow,
                    questDateUtc,
                    cancellationToken);
            }

            quest.MarkPenaltyChecked(utcNow);
            questRepository.Update(quest);
        }

        if (totalDailies > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new DailyPenaltyRolloverSummary(totalDailies, totalPenalties);
    }

    private async Task<int> TryApplyPenaltyAsync(
        Guid userId,
        DateTime utcNow,
        DateTime questDateUtc,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return 0;

        logger.LogInformation(
            "daily_quest_missed userId={UserId} questDate={QuestDate}",
            userId, questDateUtc.ToString("yyyy-MM-dd"));

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        if (accessStatus is not ("trial_active" or "subscription_active")) return 0;

        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (progression is null) return 0;

        // US-230/P0-2: Tônico de Recuperação/Selo de Proteção protegem o dia
        // ANTES de aplicar penalidade/resetar streak. Comparação sempre contra
        // questDateUtc (o dia sendo processado), nunca utcNow de execução —
        // o rollout retroativo pode processar dias antigos numa única chamada.
        if (await TryConsumeStreakSafeguardAsync(userId, questDateUtc, cancellationToken))
        {
            logger.LogInformation(
                "streak_protected userId={UserId} questDate={QuestDate}",
                userId, questDateUtc.ToString("yyyy-MM-dd"));
            return 0;
        }

        var amount = progression.ApplyDailyMissPenalty(utcNow, questDateUtc);

        // US-070 RN-002: reinicia o streak independente do valor da penalidade.
        progression.ResetStreak(utcNow);
        logger.LogInformation("streak_updated userId={UserId} streakDays=0 reason=daily_missed", userId);

        if (amount <= 0) return 0;

        logger.LogInformation(
            "xp_penalty_applied userId={UserId} amount={Amount} source=daily_quest_missed questDate={QuestDate} consecutiveMissedDays={Days}",
            userId, amount, questDateUtc.ToString("yyyy-MM-dd"), progression.ConsecutiveMissedDailyDays);

        return 1;
    }

    /// US-230: Tônico de Recuperação (dia exato) tem prioridade sobre Selo de
    /// Proteção (qualquer selo ativo cobrindo o dia) — consome no máximo um
    /// efeito por dia perdido. Retorna true se algum efeito protegeu o dia.
    private async Task<bool> TryConsumeStreakSafeguardAsync(
        Guid userId, DateTime questDateUtc, CancellationToken cancellationToken)
    {
        // Nota: a query SQL filtra Status=="active" contra o BANCO no momento da
        // consulta — num rollout retroativo que processa vários dias do MESMO
        // usuário antes do único SaveChanges do lote, um efeito já consumido em
        // memória por um dia anterior ainda "existe como ativo" no banco. O
        // identity map do EF devolve a MESMA instância tracked (já consumida em
        // memória) — por isso o "e.IsActive" abaixo é obrigatório, não redundante.
        var recoveryDays = await itemActiveEffectRepository.GetActiveByUserAndTypeAsync(
            userId, ItemEffectTypes.RecoveryDay, cancellationToken);
        var recoveryDay = recoveryDays.FirstOrDefault(e => e.IsActive && e.EffectDateUtc?.Date == questDateUtc.Date);
        if (recoveryDay is not null)
        {
            recoveryDay.Consume(dateTimeService.UtcNow);
            itemActiveEffectRepository.Update(recoveryDay);
            return true;
        }

        var protectionSeals = await itemActiveEffectRepository.GetActiveByUserAndTypeAsync(
            userId, ItemEffectTypes.StreakProtection, cancellationToken);
        // P0-2: compara contra questDateUtc (o dia sendo protegido), não utcNow.
        var protectionSeal = protectionSeals.FirstOrDefault(e => e.IsActive && e.ExpiresAtUtc > questDateUtc);
        if (protectionSeal is not null)
        {
            protectionSeal.Consume(dateTimeService.UtcNow);
            itemActiveEffectRepository.Update(protectionSeal);
            return true;
        }

        return false;
    }
}
