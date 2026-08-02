using System.Reflection;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Hunter.Queries.GetHunterProfile;
using Awaken.Application.Progression.Services;
using Awaken.Contracts.Hunter;
using Awaken.Contracts.Progression;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Hunter;

public class GetHunterProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUserDateService> _userDateService = new();
    private readonly Mock<IDailyQuestPenaltyService> _dailyQuestPenaltyService = new();
    private readonly Mock<IFeatureFlagsService> _featureFlagsService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    public GetHunterProfileQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
        _userDateService.Setup(d => d.TodayLocal).Returns(DateOnly.FromDateTime(_utcNow));
        _dailyQuestPenaltyService
            .Setup(s => s.ApplyForUserBeforeDateAsync(_userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyPenaltyRolloverSummary(0, 0));
        _questRepository
            .Setup(r => r.GetDailiesForUserBetweenDatesAsync(
                _userId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _featureFlagsService.Setup(f => f.IsPremiumCardEnabled).Returns(true);
    }

    private GetHunterProfileQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _userProfileRepository.Object,
        _subscriptionRepository.Object,
        _hunterProgressionRepository.Object,
        _questRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _userDateService.Object,
        _dailyQuestPenaltyService.Object,
        _featureFlagsService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task HandleThrowsUnauthorizedWhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleReturnsHasProgressFalseWhenHunterProgressionDoesNotExist()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        user.UpdateProfile("Hunter", "https://cdn.awaken.app/avatar.png", DateTime.UtcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasProgress.Should().BeFalse();
        result.AccessStatus.Should().Be("trial_active");
        result.CardVariant.Should().Be("trial");
        result.DisplayName.Should().Be("Hunter");
        result.AvatarUrl.Should().Be("https://cdn.awaken.app/avatar.png");
        result.HunterClass.Should().Be("beginner_hunter");
        result.Rank.Should().BeNull();
        result.Attributes.Should().BeNull();
    }

    // Bug real (conta vin.ottoni@gmail.com): usuario com onboarding concluido ha dias,
    // com quests diarias geradas e ate perdidas, mas sem linha de HunterProgression
    // (invariante quebrado de uma conta legada). A tela de progressao nao deveria
    // tratar isso como "zerado" - deve se autocurar criando a progressao default.
    [Fact]
    public async Task HandleSelfHealsMissingProgressionWhenOnboardingAlreadyComplete()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.CompleteOnboarding(_utcNow);
        user.StartTrial(_utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasProgress.Should().BeTrue();
        result.Rank.Should().Be("E");
        result.RankScore.Should().Be(6);
        result.Level.Should().Be(1);
        result.StreakDays.Should().Be(0);
        _hunterProgressionRepository.Verify(
            r => r.AddAsync(It.IsAny<HunterProgression>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReturnsNullAvatarUrlWhenUserHasNoAvatar()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task HandleReturnsFullProfileWhenTrialActiveAndProgressionExists()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        user.UpdateProfile("Hunter", "https://cdn.awaken.app/avatar.png", DateTime.UtcNow);

        var progression = HunterProgression.Create(_userId);
        progression.AddXp(50, _utcNow); // fica no Level 1 (limiar = 100)

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasProgress.Should().BeTrue();
        result.AccessStatus.Should().Be("trial_active");
        result.CardVariant.Should().Be("trial");
        result.AvatarUrl.Should().Be("https://cdn.awaken.app/avatar.png");
        result.HunterClass.Should().Be("beginner_hunter");
        result.Rank.Should().Be("E");
        result.RankScore.Should().Be(6); // todos atributos default = 1; 1*6 = 6
        result.Level.Should().Be(1);
        result.Xp.Should().Be(50);
        result.XpToNextLevel.Should().Be(100);
        result.StreakDays.Should().Be(0);
        result.Attributes.Should().BeEquivalentTo(new AttributesDto(
            1,
            1,
            1,
            1,
            1,
            1));
    }

    [Fact]
    public async Task HandleReturnsStreakCalendarDaysFromRecentDailyQuests()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        var progression = HunterProgression.Create(_userId);
        var localToday = new DateOnly(2026, 6, 18);
        var completedDate = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc);
        var missedDate = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        var completed = Quest.Create(_userId, completedDate, "pt-BR", "completed");
        completed.Complete(40, _utcNow);
        var missed = Quest.Create(_userId, missedDate, "pt-BR", "missed");
        missed.MarkPenaltyChecked(_utcNow);

        _userDateService.Setup(s => s.TodayLocal).Returns(localToday);
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _questRepository
            .Setup(r => r.GetDailiesForUserBetweenDatesAsync(
                _userId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([completed, missed]);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.StreakCalendarDays.Should().NotBeNull();
        result.StreakCalendarDays!.Single(d => d.DateUtc.Date == completedDate).Status.Should().Be("completed");
        result.StreakCalendarDays!.Single(d => d.DateUtc.Date == missedDate).Status.Should().Be("missed");
    }

    [Fact]
    public async Task HandleReturnsActualRecentDailyPenaltyXpWhenPenaltyWasApplied()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));

        var progression = HunterProgression.Create(_userId);
        progression.AddXp(5, _utcNow);
        progression.ApplyDailyMissPenalty(_utcNow, _utcNow.AddDays(-1));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.RecentDailyPenaltyXp.Should().Be(5);
        result.RecentDailyPenaltyQuestDateUtc.Should().Be(_utcNow.Date.AddDays(-1));
    }

    [Fact]
    public async Task HandleReturnsSubscriptionActiveWhenPaidPlanNotExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(30), _utcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.CardVariant.Should().Be("premium");
    }

    // US-080 — RN-001/9.2: se o recurso premium estiver desabilitado,
    // o assinante recebe o card completo padrão, não o variant premium.
    [Fact]
    public async Task HandleReturnsStandardCardVariantWhenPremiumFeatureDisabled()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(30), _utcNow);

        _featureFlagsService.Setup(f => f.IsPremiumCardEnabled).Returns(false);
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.CardVariant.Should().Be("standard");
    }

    [Fact]
    public async Task HandleReturnsTrialExpiredWhenTrialEnded()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(-1));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_expired");
        result.CardVariant.Should().BeNull();
    }

    [Fact]
    public async Task HandleReturnsSubscriptionExpiredWhenPaidPlanExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "annual", "pro_access", "rc_123", _utcNow.AddDays(-5), _utcNow.AddDays(-6));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_expired");
        result.CardVariant.Should().BeNull();
    }

    // US-079 — RN-004/CA-002: card do trial usa variante "trial", sem
    // elementos visuais premium reservados aos assinantes.
    [Fact]
    public async Task HandleReturnsTrialCardVariantWhenTrialActive()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.CardVariant.Should().Be("trial");
    }

    // US-079 — RN-005: usuário sem trial iniciado não recebe variante de card.
    [Fact]
    public async Task HandleReturnsNullCardVariantWhenUserHasNoTrial()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("no_trial");
        result.CardVariant.Should().BeNull();
    }

    // US-077 — RN-002/CA-002: o card compartilhável usa este contrato como
    // fonte de dados; ele nunca pode expor idade, peso, altura, sexo
    // biológico, limitações ou dores.
    [Fact]
    public void HunterProfileResponseDoesNotExposeSensitivePhysicalData()
    {
        var forbiddenTerms = new[]
        {
            "age", "weight", "height", "sex", "gender", "limitation", "pain",
        };

        var propertyNames = typeof(HunterProfileResponse)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant());

        foreach (var name in propertyNames)
        {
            forbiddenTerms.Should().NotContain(
                term => name.Contains(term),
                $"property '{name}' must not expose sensitive physical data (RN-002)");
        }
    }

    // US-234 RN-001/RN-002: sem imagem do Google e sem selecao manual, o
    // avatar efetivo cai no padrao do sistema para o sexo biologico
    // informado no onboarding.
    [Fact]
    public async Task HandleReturnsFemaleDefaultAvatarKeyWhenBiologicalSexIsFemininoAndNoGoogleAvatar()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(_userId, biologicalSex: "feminino"));
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.SelectedAvatarKey.Should().Be("avatar_female_default");
    }

    [Fact]
    public async Task HandleReturnsMaleDefaultAvatarKeyWhenBiologicalSexUnknownAndNoGoogleAvatar()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.SelectedAvatarKey.Should().Be("avatar_male_default");
    }

    [Fact]
    public async Task HandleDoesNotOverrideGoogleAvatarWithGenderDefault()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        user.UpdateProfile("Hunter", "https://cdn.awaken.app/avatar.png", DateTime.UtcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.AvatarUrl.Should().Be("https://cdn.awaken.app/avatar.png");
        result.SelectedAvatarKey.Should().BeNull();
        _userProfileRepository.Verify(
            r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // US-235 RN-001: apenas assinatura anual paga e ATIVA (ExpiresAt no
    // futuro) libera a moldura dourada no card do Hunter.
    [Fact]
    public async Task HandleReturnsHasAnnualGoldenFrameTrueWhenAnnualSubscriptionActive()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "annual", "pro_access", "rc_123", _utcNow.AddDays(30), _utcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasAnnualGoldenFrame.Should().BeTrue();
    }

    // US-235 RN-002: plano mensal nunca recebe a moldura dourada, mesmo
    // com a assinatura ativa.
    [Fact]
    public async Task HandleReturnsHasAnnualGoldenFrameFalseWhenMonthlySubscriptionActive()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(30), _utcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasAnnualGoldenFrame.Should().BeFalse();
    }

    // US-235 RN-003: usuario em trial nunca recebe a moldura dourada.
    [Fact]
    public async Task HandleReturnsHasAnnualGoldenFrameFalseWhenTrialActive()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Subscription.CreateTrial(_userId, _utcNow, _utcNow.AddDays(7)));
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasAnnualGoldenFrame.Should().BeFalse();
    }

    // US-235 RN-005: assinatura anual expirada (ExpiresAt no passado) nao
    // libera a moldura dourada.
    [Fact]
    public async Task HandleReturnsHasAnnualGoldenFrameFalseWhenAnnualSubscriptionExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "annual", "pro_access", "rc_123", _utcNow.AddDays(-5), _utcNow.AddDays(-6));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetHunterProfileQuery(), CancellationToken.None);

        result.HasAnnualGoldenFrame.Should().BeFalse();
    }
}
