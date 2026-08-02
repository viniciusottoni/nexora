using System.Reflection;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Admin;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence;

public class AwakenDbContext(DbContextOptions<AwakenDbContext> options, IDateTimeService dateTimeService) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<HunterProgression> HunterProgressions => Set<HunterProgression>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<QuestExercise> QuestExercises => Set<QuestExercise>();
    public DbSet<QuestLog> QuestLogs => Set<QuestLog>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ExerciseRawImport> ExerciseRawImports => Set<ExerciseRawImport>();
    public DbSet<ExerciseCatalog> ExerciseCatalogs => Set<ExerciseCatalog>();
    public DbSet<ExerciseAttributeContribution> ExerciseAttributeContributions => Set<ExerciseAttributeContribution>();
    public DbSet<ExerciseTaxonomy> ExerciseTaxonomies => Set<ExerciseTaxonomy>();
    public DbSet<ExerciseRelationship> ExerciseRelationships => Set<ExerciseRelationship>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventorySlot> InventorySlots => Set<InventorySlot>();
    public DbSet<UserWorkoutPreference> UserWorkoutPreferences => Set<UserWorkoutPreference>();
    public DbSet<RankScoreLog> RankScoreLogs => Set<RankScoreLog>();
    public DbSet<NutritionLog> NutritionLogs => Set<NutritionLog>();
    public DbSet<UserNutritionPreference> UserNutritionPreferences => Set<UserNutritionPreference>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<ShopProduct> ShopProducts => Set<ShopProduct>();
    public DbSet<IapTransactionLedger> IapTransactionLedgers => Set<IapTransactionLedger>();
    public DbSet<GoldWallet> GoldWallets => Set<GoldWallet>();
    public DbSet<GoldLedgerEntry> GoldLedgerEntries => Set<GoldLedgerEntry>();
    public DbSet<ShopOrder> ShopOrders => Set<ShopOrder>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<SupportTicketEvent> SupportTicketEvents => Set<SupportTicketEvent>();
    public DbSet<OperationalBug> OperationalBugs => Set<OperationalBug>();
    public DbSet<OperationalBugEvent> OperationalBugEvents => Set<OperationalBugEvent>();
    public DbSet<SecurityAlert> SecurityAlerts => Set<SecurityAlert>();
    public DbSet<RevenueCatEvent> RevenueCatEvents => Set<RevenueCatEvent>();
    public DbSet<ItemUsageRecord> ItemUsageRecords => Set<ItemUsageRecord>();
    public DbSet<ItemUsageRequest> ItemUsageRequests => Set<ItemUsageRequest>();
    public DbSet<ItemActiveEffect> ItemActiveEffects => Set<ItemActiveEffect>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<TrainingProgramSplit> TrainingProgramSplits => Set<TrainingProgramSplit>();
    public DbSet<TrainingSplitDay> TrainingSplitDays => Set<TrainingSplitDay>();
    public DbSet<MuscleRecoveryState> MuscleRecoveryStates => Set<MuscleRecoveryState>();
    public DbSet<WeeklyProgressionState> WeeklyProgressionStates => Set<WeeklyProgressionState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.GetType().GetProperty("UpdatedAtUtc")?.SetValue(entry.Entity, dateTimeService.UtcNow);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in ChangeTracker.Entries<BaseEntity>().Select(e => e.Entity))
            entity.ClearDomainEvents();

        return result;
    }

    /// <summary>
    /// US-227: abre uma transação explícita de banco (EF Core) que pode
    /// abranger múltiplos SaveChangesAsync subsequentes (RN-001/RN-002).
    /// </summary>
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public void ClearChangeTracker() => ChangeTracker.Clear();
}
