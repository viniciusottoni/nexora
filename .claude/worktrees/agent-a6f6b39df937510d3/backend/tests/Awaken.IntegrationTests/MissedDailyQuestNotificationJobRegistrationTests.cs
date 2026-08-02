using Awaken.Infrastructure.Jobs;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Hosting;

namespace Awaken.IntegrationTests;

public class MissedDailyQuestNotificationJobRegistrationTests
{
    private sealed class RecordingRecurringJobManager : IRecurringJobManager
    {
        public sealed record Registration(
            string RecurringJobId,
            string JobType,
            string MethodName,
            string CronExpression,
            string TimeZoneId,
            string QueueName);

        public List<Registration> Registrations { get; } = [];

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            Registrations.Add(new Registration(
                recurringJobId,
                job.Type.FullName ?? job.Type.Name,
                job.Method.Name,
                cronExpression,
                options.TimeZone?.Id ?? string.Empty,
                job.Queue ?? string.Empty));
        }

        public void Trigger(string recurringJobId) { }

        public void RemoveIfExists(string recurringJobId) { }
    }

    [Fact]
    public void RegistersRecurringJobsWithExpectedQueuesAndSchedules()
    {
        var recurringJobManager = new RecordingRecurringJobManager();

        HangfireRecurringJobRegistration.Register(recurringJobManager);

        recurringJobManager.Registrations.Should().HaveCount(5);
        recurringJobManager.Registrations.Should().ContainEquivalentOf(new
        {
            RecurringJobId = "daily-quest-penalty-rollover",
            JobType = typeof(DailyQuestPenaltyJob).FullName,
            MethodName = nameof(DailyQuestPenaltyJob.RunAsync),
            CronExpression = "5 0 * * *",
            TimeZoneId = TimeZoneInfo.Utc.Id,
            QueueName = "quests"
        });
        recurringJobManager.Registrations.Should().ContainEquivalentOf(new
        {
            RecurringJobId = "missed-daily-quest-notification",
            JobType = typeof(MissedDailyQuestNotificationJob).FullName,
            MethodName = nameof(MissedDailyQuestNotificationJob.RunAsync),
            CronExpression = "10 0 * * *",
            TimeZoneId = TimeZoneInfo.Utc.Id,
            QueueName = "notifications"
        });
        recurringJobManager.Registrations.Should().ContainEquivalentOf(new
        {
            RecurringJobId = "daily-quest-reminder",
            JobType = typeof(DailyQuestReminderJob).FullName,
            MethodName = nameof(DailyQuestReminderJob.RunAsync),
            CronExpression = "0 8 * * *",
            TimeZoneId = TimeZoneInfo.Utc.Id,
            QueueName = "notifications"
        });
        recurringJobManager.Registrations.Should().ContainEquivalentOf(new
        {
            RecurringJobId = "streak-risk-alert",
            JobType = typeof(StreakRiskAlertJob).FullName,
            MethodName = nameof(StreakRiskAlertJob.RunAsync),
            CronExpression = "0 20 * * *",
            TimeZoneId = TimeZoneInfo.Utc.Id,
            QueueName = "notifications"
        });
        // US-228: reconciliação da economia Gold a cada 6h.
        recurringJobManager.Registrations.Should().ContainEquivalentOf(new
        {
            RecurringJobId = "gold-economy-reconciliation",
            JobType = typeof(GoldEconomyReconciliationJob).FullName,
            MethodName = nameof(GoldEconomyReconciliationJob.RunAsync),
            CronExpression = "0 */6 * * *",
            TimeZoneId = TimeZoneInfo.Utc.Id,
            QueueName = "default"
        });
    }
}
