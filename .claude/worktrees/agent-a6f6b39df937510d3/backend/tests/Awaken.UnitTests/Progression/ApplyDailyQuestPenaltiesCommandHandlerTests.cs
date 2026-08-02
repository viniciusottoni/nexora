using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Commands.ApplyDailyQuestPenalties;
using Awaken.Application.Progression.Services;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Progression;

public class ApplyDailyQuestPenaltiesCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IItemActiveEffectRepository> _itemActiveEffectRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<DailyQuestPenaltyService>> _logger = new();

    private static readonly DateOnly Today = new(2026, 6, 23);
    private static readonly DateTime UtcNow = new(2026, 6, 23, 0, 5, 0, DateTimeKind.Utc);
    private static readonly DateTime YesterdayUtc = new(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);

    public ApplyDailyQuestPenaltiesCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.TodayUtc).Returns(Today);
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        // US-230: sem Selo/Tônico ativos por padrão — testes específicos de
        // proteção de streak sobrescrevem este setup.
        _itemActiveEffectRepository
            .Setup(r => r.GetActiveByUserAndTypeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private ApplyDailyQuestPenaltiesCommandHandler CreateHandler()
    {
        var service = new DailyQuestPenaltyService(
            _questRepository.Object,
            _userRepository.Object,
            _subscriptionRepository.Object,
            _hunterProgressionRepository.Object,
            _itemActiveEffectRepository.Object,
            _dateTimeService.Object,
            _unitOfWork.Object,
            _logger.Object);

        return new ApplyDailyQuestPenaltiesCommandHandler(service, _dateTimeService.Object);
    }

    private static Quest MissedDailyFor(Guid userId) =>
        Quest.Create(userId, YesterdayUtc, "pt-BR", $"{userId:N}_20260622_daily");

    // CA-001 (US-129) / RN-001 (US-132): daily nao completada + acesso ativo aciona a penalidade.
    [Fact]
    public async Task Handle_AppliesPenalty_WhenDailyMissedAndTrialActive()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(50, UtcNow); // fica no Level 1 (limiar = 100), TotalXp = 50

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        result.MissedDailiesChecked.Should().Be(1);
        result.PenaltiesApplied.Should().Be(1);
        progression.TotalXp.Should().Be(40); // 50 - 10
        progression.RecentDailyPenaltyXp.Should().Be(10);
        progression.RecentDailyPenaltyQuestDateUtc.Should().Be(YesterdayUtc);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // US-129/CA-001: se o XP cair no piso, a penalidade mostrada ao usuario
    // deve refletir o valor realmente aplicado, nao o valor teorico.
    [Fact]
    public async Task Handle_StoresActualPenaltyAmount_WhenXpHitsFloor()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(5, UtcNow);

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        result.PenaltiesApplied.Should().Be(1);
        progression.TotalXp.Should().Be(0);
        progression.RecentDailyPenaltyXp.Should().Be(5);
        progression.RecentDailyPenaltyQuestDateUtc.Should().Be(YesterdayUtc);
    }

    // CA-002 (US-129) / RN-004 (US-132): sem acesso ativo, nenhuma penalidade e acionada.
    [Fact]
    public async Task Handle_AppliesNoPenalty_WhenTrialExpired()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-1));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(50, UtcNow); // fica no Level 1 (limiar = 100), TotalXp = 50

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        result.PenaltiesApplied.Should().Be(0);
        progression.TotalXp.Should().Be(50);
    }

    // US-129/secao 10: daily completada nao aciona penalidade.
    [Fact]
    public async Task Handle_AppliesNoPenalty_WhenDailyCompleted()
    {
        var userId = Guid.NewGuid();
        var quest = MissedDailyFor(userId);
        quest.Complete(40, UtcNow);

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([quest]);

        var result = await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        result.PenaltiesApplied.Should().Be(0);
        _userRepository.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
        _hunterProgressionRepository.Verify(
            r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    // Sem progressao registrada ainda: nada para penalizar.
    [Fact]
    public async Task Handle_AppliesNoPenalty_WhenProgressionDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        result.PenaltiesApplied.Should().Be(0);
    }

    // US-070 / CA-001 / RN-002: daily não completada reinicia o streak para usuário ativo.
    [Fact]
    public async Task Handle_ResetsStreak_WhenDailyMissedAndTrialActive()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(50, UtcNow);
        // Simula 3 dias de streak antes da miss
        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-3));
        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-2));
        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-1));
        progression.CurrentStreakDays.Should().Be(3);

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        progression.CurrentStreakDays.Should().Be(0);
    }

    // US-070 / 9.1: sem acesso ativo, streak não é resetado (usuário expirado não é processado).
    [Fact]
    public async Task Handle_DoesNotResetStreak_WhenTrialExpired()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-1)); // trial expirado
        var progression = HunterProgression.Create(userId);
        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-1));
        progression.CurrentStreakDays.Should().Be(1);

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        // Sem acesso ativo → não chegou ao progression; streak intacto.
        progression.CurrentStreakDays.Should().Be(1);
    }

    // US-137: log daily_quest_missed deve incluir questDate.
    [Fact]
    public async Task US137_LogsDailyQuestMissed_WithQuestDate_WhenDailyMissedAndTrialActive()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(50, UtcNow);

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("daily_quest_missed") &&
                    v.ToString()!.Contains($"questDate={YesterdayUtc:yyyy-MM-dd}") &&
                    !v.ToString()!.Contains("xp_penalty_applied")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // US-137: log xp_penalty_applied deve incluir source e questDate.
    [Fact]
    public async Task US137_LogsXpPenaltyApplied_WithSourceAndQuestDate_WhenDailyMissedAndTrialActive()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(50, UtcNow);

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("xp_penalty_applied") &&
                    v.ToString()!.Contains("source=daily_quest_missed") &&
                    v.ToString()!.Contains($"questDate={YesterdayUtc:yyyy-MM-dd}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // US-230: Selo de Proteção ativo protege o dia perdido — sem penalidade,
    // sem reset de streak, e o efeito é consumido (Status vira "consumed").
    [Fact]
    public async Task Handle_SkipsPenaltyAndConsumesSeal_WhenProtectionSealActive()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        var progression = HunterProgression.Create(userId);
        progression.AddXp(50, UtcNow);
        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-1));
        progression.CurrentStreakDays.Should().Be(1);

        var seal = ItemActiveEffect.Create(
            userId, ItemKeys.ProtectionSeal, ItemEffectTypes.StreakProtection,
            activatedAtUtc: UtcNow.AddDays(-2), expiresAtUtc: UtcNow.AddDays(28));

        _questRepository.Setup(r => r.GetUncheckedDailiesPageAsync(YesterdayUtc, null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MissedDailyFor(userId)]);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _itemActiveEffectRepository
            .Setup(r => r.GetActiveByUserAndTypeAsync(userId, ItemEffectTypes.StreakProtection, It.IsAny<CancellationToken>()))
            .ReturnsAsync([seal]);

        var result = await CreateHandler().Handle(new ApplyDailyQuestPenaltiesCommand(), CancellationToken.None);

        result.PenaltiesApplied.Should().Be(0);
        progression.CurrentStreakDays.Should().Be(1); // não resetou
        progression.TotalXp.Should().Be(50); // sem penalidade de XP
        seal.IsActive.Should().BeFalse(); // consumido
        _itemActiveEffectRepository.Verify(r => r.Update(seal), Times.Once);
    }
}
