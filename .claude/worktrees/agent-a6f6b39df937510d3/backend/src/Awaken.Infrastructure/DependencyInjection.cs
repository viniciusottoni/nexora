using Amazon.S3;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Cache;
using Awaken.Infrastructure.ItemEffects;
using Awaken.Infrastructure.Jobs;
using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Persistence.Repositories;
using Awaken.Infrastructure.Services;
using Awaken.Application.Progression.Services;
using Awaken.Application.Quests.Common;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Awaken.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AwakenDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("PostgreSQL"),
                b => b.MigrationsAssembly(typeof(AwakenDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AwakenDbContext>());

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConnection = config.GetConnectionString("Redis")!;
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 1000;
            options.SyncTimeout = 1000;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped<IAccessStatusCacheService, AccessStatusCacheService>();
        services.AddScoped<ExerciseCatalogCacheService>();
        services.AddScoped<IExerciseCatalogCacheService>(sp => sp.GetRequiredService<ExerciseCatalogCacheService>());
        services.AddScoped<ShopProductCacheService>();
        services.AddScoped<IShopProductCacheService>(sp => sp.GetRequiredService<ShopProductCacheService>());
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IUserDateService, UserDateService>();
        services.AddScoped<IFeatureFlagsService, FeatureFlagsService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<ILoginAttemptTracker, LoginAttemptTracker>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<DailyWorkoutBlueprintBuilder>();
        services.AddScoped<IWorkoutGeneratorService, WorkoutGeneratorService>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IHunterProgressionRepository, HunterProgressionRepository>();
        services.AddScoped<IExerciseRawImportRepository, ExerciseRawImportRepository>();
        services.AddScoped<IExerciseCatalogRepository, ExerciseCatalogRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<IQuestLogRepository, QuestLogRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventorySlotRepository, InventorySlotRepository>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IItemUsageRecordRepository, ItemUsageRecordRepository>();
        services.AddScoped<IItemUsageRequestRepository, ItemUsageRequestRepository>();
        services.AddScoped<IItemActiveEffectRepository, ItemActiveEffectRepository>();

        // US-230: handlers de efeito de item — registrados como IItemEffectHandler
        // (coleção injetada no UseItemCommandHandler) + DefaultItemEffectHandler
        // individualmente como fallback.
        services.AddScoped<IItemEffectHandler, ReforgeScrollEffectHandler>();
        services.AddScoped<IItemEffectHandler, SubstitutionScrollEffectHandler>();
        services.AddScoped<IItemEffectHandler, DungeonCompassEffectHandler>();
        services.AddScoped<IItemEffectHandler, DungeonKeyEffectHandler>();
        services.AddScoped<IItemEffectHandler, ProtectionSealEffectHandler>();
        services.AddScoped<IItemEffectHandler, RecoveryTonicEffectHandler>();
        services.AddScoped<IItemEffectHandler, ReturnAmuletEffectHandler>();
        services.AddScoped<IItemEffectHandler, FocusPotionEffectHandler>();
        services.AddScoped<IItemEffectHandler, FocusPotionLargeEffectHandler>();
        services.AddScoped<IItemEffectHandler, LuckPotionEffectHandler>();
        services.AddScoped<IItemEffectHandler, DungeonStoneEffectHandler>();
        services.AddScoped<IItemEffectHandler, ScrollRenameEffectHandler>();
        services.AddScoped<IItemEffectHandler, ScrollClassChangeEffectHandler>();
        services.AddScoped<IItemEffectHandler, PackStrikerEffectHandler>();
        services.AddScoped<IItemEffectHandler, PackRunnerEffectHandler>();
        services.AddScoped<IItemEffectHandler, PackGuardianEffectHandler>();
        services.AddScoped<IItemEffectHandler, PackShadowEffectHandler>();
        services.AddScoped<IItemEffectHandler, PackReawakenedEffectHandler>();
        // Fallback: sempre registrado por último para que FirstOrDefault por ItemKey == "*"
        // seja encontrado quando nenhum handler específico existir (cosméticos, packs, slots).
        services.AddScoped<IItemEffectHandler, DefaultItemEffectHandler>();
        services.AddScoped<IUserWorkoutPreferenceRepository, UserWorkoutPreferenceRepository>();
        services.AddScoped<IRankScoreLogRepository, RankScoreLogRepository>();
        services.AddScoped<INutritionLogRepository, NutritionLogRepository>();
        services.AddScoped<IUserNutritionPreferenceRepository, UserNutritionPreferenceRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationEligibilityService, NotificationEligibilityService>();
        services.AddScoped<IDailyQuestPenaltyService, DailyQuestPenaltyService>();
        services.AddScoped<IQuestRegenerationService, QuestRegenerationService>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<IShopProductRepository, ShopProductRepository>();
        services.AddScoped<ITrainingProgramRepository, TrainingProgramRepository>();
        services.AddScoped<ITrainingProgramSplitRepository, TrainingProgramSplitRepository>();
        services.AddScoped<IMuscleRecoveryStateRepository, MuscleRecoveryStateRepository>();
        services.AddScoped<IWeeklyProgressionStateRepository, WeeklyProgressionStateRepository>();
        services.AddScoped<Awaken.Application.Progression.Common.WeeklyProgressionReviewer>();
        services.AddScoped<IIapTransactionLedgerRepository, IapTransactionLedgerRepository>();
        services.AddScoped<IGoldWalletRepository, GoldWalletRepository>();
        services.AddScoped<IGoldLedgerEntryRepository, GoldLedgerEntryRepository>();
        services.AddScoped<IGoldWalletService, GoldWalletService>();
        services.AddScoped<IShopOrderRepository, ShopOrderRepository>();
        services.AddScoped<IRevenueCatEventRepository, RevenueCatEventRepository>();

        // US-195: RevenueCat transaction validation service.
        services.AddScoped<IRevenueCatValidationService, RevenueCatValidationService>();
        services.AddHttpClient();

        // US-200: safe directory resolver â€” restricts exercise import to a configured root directory.
        services.AddSingleton<ISafeDirectoryResolver, SafeDirectoryResolver>();

        // US-236: cliente S3 apontando para o bucket compativel com S3 configurado (ADR-024),
        // usado pelo S3MediaStorageService para publicar os GIFs 360 dos exercicios.
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var endpoint = config["Storage:Endpoint"];
            var accessKey = config["Storage:AccessKey"];
            var secretKey = config["Storage:SecretKey"];
            var s3Config = new AmazonS3Config
            {
                ServiceURL = string.IsNullOrWhiteSpace(endpoint) ? "https://localhost" : endpoint,
                ForcePathStyle = true,
            };

            return new AmazonS3Client(accessKey ?? string.Empty, secretKey ?? string.Empty, s3Config);
        });
        services.AddScoped<IMediaStorageService, S3MediaStorageService>();
        services.AddScoped<IMediaRedirectService, S3MediaRedirectService>();

        // EPIC-017: site admin â€” auth, tickets triagem, bugs operacionais, alertas de seguranca.
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminUserQueryRepository, AdminUserQueryRepository>();
        services.AddScoped<ISupportTicketEventRepository, SupportTicketEventRepository>();
        services.AddScoped<IOperationalBugRepository, OperationalBugRepository>();
        services.AddScoped<IOperationalBugEventRepository, OperationalBugEventRepository>();
        services.AddScoped<ISecurityAlertRepository, SecurityAlertRepository>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IAdminJwtService, AdminJwtService>();
        services.AddScoped<ICurrentAdminService, CurrentAdminService>();

        // EPIC-017 (US-216 a US-224): diagnosticos operacionais e readiness do MVP no Admin.
        services.AddScoped<IAdminSubscriptionDiagnosticsRepository, AdminSubscriptionDiagnosticsRepository>();
        services.AddScoped<IReadinessCheckService, ReadinessCheckService>();
        services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();
        services.AddScoped<IJobMonitoringService, JobMonitoringService>();
        services.AddScoped<IMediaDiagnosticsService, MediaDiagnosticsService>();
        services.AddScoped<IMvpHealthService, MvpHealthService>();
        services.AddScoped<IOperationalTimelineService, OperationalTimelineService>();

        // US-092: push notifications via Firebase.
        services.AddSingleton<IPushNotificationService, FirebasePushNotificationService>();

        // US-129: job de virada de dia (penalidade de XP por daily nao completada).
        // AddHangfireServer() is intentionally omitted here â€” the worker process registers it.
        services.AddHangfire(cfg => cfg
            .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(config.GetConnectionString("PostgreSQL"))));
        services.AddScoped<DailyQuestPenaltyJob>();
        services.AddScoped<MissedDailyQuestNotificationJob>();

        // US-092: job de lembrete de quest diaria.
        services.AddScoped<DailyQuestReminderJob>();

        // US-093: job de alerta de streak em risco.
        services.AddScoped<StreakRiskAlertJob>();

        return services;
    }
}
