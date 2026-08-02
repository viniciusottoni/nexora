using Hangfire;

namespace Awaken.Infrastructure.Jobs;

public static class HangfireRecurringJobRegistration
{
    private const string QuestsQueue = "quests";
    private const string NotificationsQueue = "notifications";
    private const string DefaultQueue = "default";

    public static string[] ServerQueues { get; } = [QuestsQueue, NotificationsQueue, DefaultQueue];

    public static void Register(IRecurringJobManager recurringJobManager)
    {
        recurringJobManager.AddOrUpdate<DailyQuestPenaltyJob>(
            "daily-quest-penalty-rollover",
            QuestsQueue,
            job => job.RunAsync(CancellationToken.None),
            "5 0 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // US-135: aviso de quest perdida roda depois da penalidade de XP, ainda na virada de dia.
        recurringJobManager.AddOrUpdate<MissedDailyQuestNotificationJob>(
            "missed-daily-quest-notification",
            NotificationsQueue,
            job => job.RunAsync(CancellationToken.None),
            "10 0 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // US-092: lembrete de quest diaria executa diariamente as 08:00 UTC.
        recurringJobManager.AddOrUpdate<DailyQuestReminderJob>(
            "daily-quest-reminder",
            NotificationsQueue,
            job => job.RunAsync(CancellationToken.None),
            "0 8 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // US-093: alerta de streak em risco executa diariamente as 20:00 UTC.
        recurringJobManager.AddOrUpdate<StreakRiskAlertJob>(
            "streak-risk-alert",
            NotificationsQueue,
            job => job.RunAsync(CancellationToken.None),
            "0 20 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // US-228: reconciliação da economia Gold a cada 6h — frequência suficiente para
        // detectar divergências/abuso rapidamente sem sobrecarregar o banco com leitura
        // completa de wallets/ledger/pedidos/inventário (ainda sem paginação no MVP).
        recurringJobManager.AddOrUpdate<GoldEconomyReconciliationJob>(
            "gold-economy-reconciliation",
            DefaultQueue,
            job => job.RunAsync(CancellationToken.None),
            "0 */6 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }
}
