using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Services;
using Awaken.Contracts.Hunter;
using Awaken.Contracts.Progression;
using Awaken.Domain.Entities.Avatars;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using MediatR;


namespace Awaken.Application.Hunter.Queries.GetHunterProfile;

public class GetHunterProfileQueryHandler(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    ISubscriptionRepository subscriptionRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUserDateService userDateService,
    IDailyQuestPenaltyService dailyQuestPenaltyService,
    IFeatureFlagsService featureFlagsService,
    IUnitOfWork unitOfWork) : IRequestHandler<GetHunterProfileQuery, HunterProfileResponse>
{
    public async Task<HunterProfileResponse> Handle(
        GetHunterProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var todayLocalQuestDateUtc = DateTime.SpecifyKind(
            userDateService.TodayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        await dailyQuestPenaltyService.ApplyForUserBeforeDateAsync(
            userId,
            todayLocalQuestDateUtc,
            cancellationToken);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        // US-234 RN-001/RN-002: sem selecao manual e sem imagem do Google, o
        // avatar efetivo e o padrao do sistema para o sexo biologico
        // informado no onboarding (UserProfile pode nao existir ainda).
        string? effectiveSelectedAvatarKey = user.SelectedAvatarKey;
        if (effectiveSelectedAvatarKey is null && string.IsNullOrEmpty(user.AvatarUrl))
        {
            var onboardingProfile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            effectiveSelectedAvatarKey = AvatarCatalog.GenderDefaultAvatarKey(onboardingProfile?.BiologicalSex);
        }

        var utcNow = dateTimeService.UtcNow;
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        var cardVariant = accessStatus switch
        {
            "trial_active" => "trial",
            "subscription_active" => featureFlagsService.IsPremiumCardEnabled ? "premium" : "standard",
            _ => null,
        };

        // US-235 RN-001/RN-002/RN-003/RN-005: só plano anual com assinatura
        // paga ativa (nunca mensal ou trial) recebe a moldura dourada.
        var hasAnnualGoldenFrame = subscription?.Plan == "annual" && accessStatus == "subscription_active";

        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);

        // Auto-cura: onboarding concluido implica HunterProgression existente (criada em
        // CompleteOnboardingCommandHandler). Contas legadas anteriores a esse invariante
        // podem ter ficado sem a linha - sem isto, o app mostraria "sem progresso" para
        // sempre e outros handlers (ex.: CompleteQuestCommandHandler) nunca atualizariam
        // rank/streak/atributos dessas contas. Cria e persiste uma vez, com defaults.
        if (progression is null && user.IsOnboardingComplete)
        {
            progression = HunterProgression.Create(userId);
            await hunterProgressionRepository.AddAsync(progression, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (progression is null)
        {
            return new HunterProfileResponse(
                AccessStatus: accessStatus,
                HasProgress: false,
                CardVariant: cardVariant,
                HasAnnualGoldenFrame: hasAnnualGoldenFrame,
                DisplayName: user.DisplayName,
                AvatarUrl: user.AvatarUrl,
                SelectedAvatarKey: effectiveSelectedAvatarKey,
                HunterClass: HunterProgression.DefaultHunterClass);
        }

        var streakCalendarDays = await BuildStreakCalendarDaysAsync(
            userId,
            todayLocalQuestDateUtc,
            cancellationToken);

        return new HunterProfileResponse(
            AccessStatus: accessStatus,
            HasProgress: true,
            CardVariant: cardVariant,
            HasAnnualGoldenFrame: hasAnnualGoldenFrame,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            SelectedAvatarKey: effectiveSelectedAvatarKey,
            HunterClass: progression.HunterClass,
            Rank: progression.Rank,
            RankScore: progression.RankScore,
            Level: progression.Level,
            Xp: progression.TotalXp,
            XpToNextLevel: progression.XpToNextLevel,
            StreakDays: progression.CurrentStreakDays,
            Attributes: new AttributesDto(
                progression.Strength,
                progression.Endurance,
                progression.Agility,
                progression.Vitality,
                progression.Focus,
                progression.Wisdom),
            RecentDailyPenaltyXp: progression.RecentDailyPenaltyXp is > 0
                ? progression.RecentDailyPenaltyXp
                : null,
            RecentDailyPenaltyQuestDateUtc: progression.RecentDailyPenaltyQuestDateUtc is not null
                ? progression.RecentDailyPenaltyQuestDateUtc
                : null,
            AttributeXp: new AttributeXpDto(
                progression.StrengthXp,
                progression.AgilityXp,
                progression.EnduranceXp,
                progression.VitalityXp,
                progression.FocusXp,
                progression.WisdomXp),
            StreakCalendarDays: streakCalendarDays,
            EquippedFrameKey: progression.EquippedFrameKey,
            EquippedAuraKey: progression.EquippedAuraKey,
            EquippedBackgroundKey: progression.EquippedBackgroundKey);
    }

    private async Task<IReadOnlyList<StreakCalendarDayResponse>> BuildStreakCalendarDaysAsync(
        Guid userId,
        DateTime todayLocalQuestDateUtc,
        CancellationToken cancellationToken)
    {
        const int CalendarDays = 14;
        var fromDateUtc = todayLocalQuestDateUtc.AddDays(-(CalendarDays - 1));
        var quests = await questRepository.GetDailiesForUserBetweenDatesAsync(
            userId,
            fromDateUtc,
            todayLocalQuestDateUtc,
            cancellationToken);

        var questsByDate = quests
            .GroupBy(q => q.QuestDateUtc.Date)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(q => q.Status == "completed").First());

        var days = new List<StreakCalendarDayResponse>(CalendarDays);
        for (var date = fromDateUtc.Date; date <= todayLocalQuestDateUtc.Date; date = date.AddDays(1))
        {
            var status = "none";
            if (questsByDate.TryGetValue(date, out var quest))
            {
                status = quest.Status == "completed"
                    ? "completed"
                    : date < todayLocalQuestDateUtc.Date && quest.PenaltyCheckedAtUtc is not null
                        ? "missed"
                        : "pending";
            }

            days.Add(new StreakCalendarDayResponse(DateTime.SpecifyKind(date, DateTimeKind.Utc), status));
        }

        return days;
    }
}
