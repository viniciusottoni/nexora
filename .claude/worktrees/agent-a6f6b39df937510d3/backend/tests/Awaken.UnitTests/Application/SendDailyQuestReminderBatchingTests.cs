// US-207: testa o comportamento de batching do job de lembrete de quest diaria.
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendDailyQuestReminder;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Application;

public class SendDailyQuestReminderBatchingTests
{
    private readonly Mock<INotificationPreferenceRepository> _notificationPreferenceRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IPushNotificationService> _pushNotificationService = new();
    private readonly Mock<INotificationLogRepository> _notificationLogRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendDailyQuestReminderCommandHandler>> _logger = new();

    private readonly DateTime _utcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);
    private readonly DateOnly _today = new(2026, 6, 29);

    public SendDailyQuestReminderBatchingTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
        _dateTimeService.Setup(d => d.TodayUtc).Returns(_today);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _notificationLogRepository
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private SendDailyQuestReminderCommandHandler CreateHandler() => new(
        _notificationPreferenceRepository.Object,
        _userRepository.Object,
        _subscriptionRepository.Object,
        _questRepository.Object,
        _pushNotificationService.Object,
        _notificationLogRepository.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static NotificationPreference BuildEnabledPreference(Guid userId)
    {
        var pref = NotificationPreference.Create(userId, true, "token_" + userId.ToString("N"), "token_registered", DateTime.UtcNow);
        return pref;
    }

    // ─── Empty first page ───────────────────────────────────────────────────

    [Fact]
    public async Task EmptyFirstPage_JobCompletes_WithZeroResults()
    {
        // Arrange: primeiro batch vazio — nenhum usuario com push habilitado
        _notificationPreferenceRepository
            .Setup(r => r.GetPageWithPushEnabledAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        // Assert
        result.Eligible.Should().Be(0);
        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(0);

        // SaveChanges nao deve ser chamado se nao houve batch processado
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Two pages processed ────────────────────────────────────────────────

    [Fact]
    public async Task TwoPages_BothAreProcessed_AndSaveCalledPerBatch()
    {
        // Arrange: usuarios com acesso expirado para simplificar (apenas contamos os processados)
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var pref1 = BuildEnabledPreference(userId1);
        var pref2 = BuildEnabledPreference(userId2);

        // Pagina 1: 1 item (= PageSize 1 para forcar segunda chamada)
        // Pagina 2: 1 item (< PageSize — ultima pagina)
        _notificationPreferenceRepository
            .SetupSequence(r => r.GetPageWithPushEnabledAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([pref1])   // pagina 1
            .ReturnsAsync([pref2])   // pagina 2
            .ReturnsAsync([]);       // safety — nao deve ser chamada

        // Ambos os usuarios nao existem — resultado: skipped
        _userRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Auth.User?)null);

        // Act: usa PageSize de 1 via reflection nao e possivel; mas o handler usa 500.
        // Para esse teste, configuramos o mock para retornar [pref] e entao [] — o handler
        // vera preferences.Count (1) < PageSize (500) e parara apos a primeira pagina.
        // Para testar duas paginas, precisamos simular que a primeira pagina tem exatamente PageSize (500) itens.
        // Como nao podemos alterar a constante, vamos verificar que ambas as prefs foram processadas
        // via o total de chamadas a GetByIdAsync.

        // Reset e reconfigure com PageSize items na primeira pagina
        _notificationPreferenceRepository.Reset();
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _notificationLogRepository
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Pagina 1: PageSize (500) itens — para forcar o loop a continuar
        var page1 = Enumerable.Range(0, 500)
            .Select(_ => BuildEnabledPreference(Guid.NewGuid()))
            .ToList();
        // Pagina 2: 1 item (< PageSize — ultima pagina)
        var page2 = new List<NotificationPreference> { pref2 };

        _notificationPreferenceRepository
            .SetupSequence(r => r.GetPageWithPushEnabledAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page1)
            .ReturnsAsync(page2)
            .ReturnsAsync([]);

        _userRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Auth.User?)null);

        // Act
        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        // Assert: total = 500 + 1 = 501 processados (todos skipped, usuario nao encontrado)
        result.Eligible.Should().Be(501);
        result.Skipped.Should().Be(501);
        result.Sent.Should().Be(0);

        // SaveChanges deve ser chamado uma vez por batch (2 batches)
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ─── Push failure for one user — job continues ──────────────────────────

    [Fact]
    public async Task PushFailureForOneUser_DoesNotAbortLoop_OtherUsersProcessed()
    {
        // Arrange: dois usuarios, primeiro falha no push, segundo nao chega ao push (access inativo)
        var failingUserId = Guid.NewGuid();
        var inactiveUserId = Guid.NewGuid();
        var failingPref = BuildEnabledPreference(failingUserId);
        var inactivePref = BuildEnabledPreference(inactiveUserId);

        _notificationPreferenceRepository
            .SetupSequence(r => r.GetPageWithPushEnabledAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([failingPref, inactivePref])
            .ReturnsAsync([]);

        // failingUser: existe mas push falha
        var failingUser = Awaken.Domain.Entities.Auth.User.Create(
            "failing@awaken.app", "hash", "Failing", "pt-BR");
        // Seta TrialEndsAt no futuro para ter acesso ativo
        typeof(Awaken.Domain.Entities.Auth.User)
            .GetProperty(nameof(Awaken.Domain.Entities.Auth.User.TrialEndsAt))!
            .SetValue(failingUser, _utcNow.AddDays(7));

        _userRepository.Setup(r => r.GetByIdAsync(failingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failingUser);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(failingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Subscriptions.Subscription?)null);
        _questRepository.Setup(r => r.GetByUserIdAndDateAsync(failingUserId, "daily", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Quests.Quest?)null);
        _pushNotificationService.Setup(p => p.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("push_send_failed"));

        // inactiveUser: nao existe → skipped imediatamente
        _userRepository.Setup(r => r.GetByIdAsync(inactiveUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Auth.User?)null);

        // Act
        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        // Assert: ambos processados, failing=skipped(failed), inactive=skipped(user not found)
        result.Eligible.Should().Be(2);
        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(2);

        // SaveChanges chamado 1 vez (um batch, < PageSize)
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
