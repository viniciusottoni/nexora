using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Inventory.Commands.UseItem;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Inventory;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.ItemEffects;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Inventory;

public class UseItemCommandHandlerTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IItemUsageRecordRepository> _usageRecordRepository = new();
    private readonly Mock<IItemUsageRequestRepository> _usageRequestRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUserDateService> _userDateService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<UseItemCommandHandler>> _logger = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<IQuestRegenerationService> _questRegenerationService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    public UseItemCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _userDateService.Setup(s => s.TodayLocal).Returns(DateOnly.FromDateTime(UtcNow));
        _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        // Sem replay de idempotência por padrão (RN-003) — testes específicos sobrescrevem.
        _usageRequestRepository
            .Setup(r => r.GetByUseRequestIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ItemUsageRequest?)null);

        // ReforgeScrollEffectHandler resolve IQuestRegenerationService via context.Services.
        _serviceProvider
            .Setup(sp => sp.GetService(typeof(IQuestRegenerationService)))
            .Returns(_questRegenerationService.Object);
        _questRegenerationService
            .Setup(s => s.RegenerateAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Quest.Create(UserId, UtcNow.Date, "pt-BR", "idempotency-key"));
    }

    private UseItemCommandHandler CreateHandler(IEnumerable<IItemEffectHandler>? handlers = null)
    {
        // Default set: specific handler + fallback handler (ItemKey == "*").
        var effectHandlers = handlers ?? new IItemEffectHandler[]
        {
            new ReforgeScrollEffectHandler(),
            new DefaultItemEffectHandler()
        };

        return new UseItemCommandHandler(
            _inventoryRepository.Object,
            _usageRecordRepository.Object,
            _usageRequestRepository.Object,
            effectHandlers,
            _currentUserService.Object,
            _dateTimeService.Object,
            _userDateService.Object,
            _unitOfWork.Object,
            _httpContextAccessor.Object,
            _auditLogService.Object,
            _logger.Object,
            _serviceProvider.Object);
    }

    private static UseItemCommand CreateCommand(string itemKey = ItemKeys.ReforgeScroll, string useRequestId = "use-request-id-001")
        => new(itemKey, null, null, useRequestId);

    // ─── Happy path ───────────────────────────────────────────────────────────

    /// US-230 / CA-001: uso bem-sucedido do reforja_scroll (consumível com limite diário).
    [Fact]
    public async Task UseItem_ConsumesItem_WhenValidAndWithinLimit()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 2);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Sem registro de uso anterior neste período.
        _usageRecordRepository
            .Setup(r => r.GetAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ItemUsageRecord?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ItemKey.Should().Be(ItemKeys.ReforgeScroll);
        result.EffectApplied.Should().BeTrue();
        result.EffectType.Should().Be("quest_regenerated");
        result.NewQuantity.Should().Be(1); // consumiu 1 de 2

        _inventoryRepository.Verify(r => r.Update(item), Times.Once);
        _usageRecordRepository.Verify(
            r => r.AddAsync(It.IsAny<ItemUsageRecord>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _usageRequestRepository.Verify(
            r => r.AddAsync(It.IsAny<ItemUsageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Item não encontrado ──────────────────────────────────────────────────

    /// US-230 / RN-001: item não existe no inventário → NotFoundException.
    [Fact]
    public async Task UseItem_Throws_WhenItemNotInInventory()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var act = () => CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Quantidade zero ──────────────────────────────────────────────────────

    /// US-230 / RN-002: item existe mas quantidade é zero → NotFoundException.
    [Fact]
    public async Task UseItem_Throws_WhenQuantityIsZero()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 0);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var act = () => CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Limite de uso excedido ───────────────────────────────────────────────

    /// US-230 / RN-003: limite diário de uso atingido → UsageLimitExceededException.
    [Fact]
    public async Task UseItem_Throws_WhenDailyLimitExceeded()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 3);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Simula registro de uso existente com UsageCount já no limite (1).
        var existingUsage = ItemUsageRecord.Create(
            UserId, ItemKeys.ReforgeScroll, UtcNow.Date, UtcNow);

        _usageRecordRepository
            .Setup(r => r.GetAsync(UserId, ItemKeys.ReforgeScroll, UtcNow.Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUsage);

        var act = () => CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UsageLimitExceededException>()
            .Where(ex => ex.ItemKey == ItemKeys.ReforgeScroll && ex.Limit == 1);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Handler sem consumo ──────────────────────────────────────────────────

    /// US-230 / CA-002: handler com ConsumesOnUse=false não chama Update no inventário.
    [Fact]
    public async Task UseItem_DoesNotConsumeItem_WhenHandlerSaysNotToConsume()
    {
        const string itemKey = "item_no_consume";
        var item = InventoryItem.Create(UserId, itemKey, quantity: 5);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, itemKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var noConsumeHandler = new NoConsumeTestHandler(itemKey);
        var handler = CreateHandler(handlers: [noConsumeHandler]);

        var result = await handler.Handle(new UseItemCommand(itemKey, null, null, "req-no-consume"), CancellationToken.None);

        result.EffectApplied.Should().BeTrue();
        result.NewQuantity.Should().Be(5); // quantidade inalterada

        _inventoryRepository.Verify(r => r.Update(It.IsAny<InventoryItem>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Fallback handler ─────────────────────────────────────────────────────

    /// US-230 / CA-003: item sem handler específico usa DefaultItemEffectHandler (key "*").
    [Fact]
    public async Task UseItem_UsesDefaultHandler_WhenNoSpecificHandlerRegistered()
    {
        const string cosmetic = ItemKeys.AuraDefault;
        var item = InventoryItem.Create(UserId, cosmetic, quantity: 1);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, cosmetic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Apenas o reforja_scroll como handler específico + o fallback default.
        var result = await CreateHandler(handlers: new IItemEffectHandler[]
            {
                new ReforgeScrollEffectHandler(),
                new DefaultItemEffectHandler()
            })
            .Handle(new UseItemCommand(cosmetic, null, null, "req-cosmetic"), CancellationToken.None);

        result.EffectType.Should().Be("item_applied");
        result.EffectApplied.Should().BeTrue();
        result.NewQuantity.Should().Be(0); // consumiu 1 de 1

        _inventoryRepository.Verify(r => r.Update(item), Times.Once);
    }

    // ─── Idempotência (RN-003) ────────────────────────────────────────────────

    /// US-230 / RN-003: reenvio do mesmo UseRequestId retorna a resposta salva
    /// sem reaplicar o efeito nem consumir o item novamente.
    [Fact]
    public async Task UseItem_ReturnsStoredResponse_WhenUseRequestIdAlreadyProcessed()
    {
        const string useRequestId = "req-replay-001";
        var existingRequest = ItemUsageRequest.Create(
            UserId, ItemKeys.ReforgeScroll, useRequestId,
            success: true, effectType: "quest_regenerated", message: null,
            remainingQuantity: 1, UtcNow);

        _usageRequestRepository
            .Setup(r => r.GetByUseRequestIdAsync(useRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRequest);

        var result = await CreateHandler().Handle(
            CreateCommand(useRequestId: useRequestId), CancellationToken.None);

        result.EffectApplied.Should().BeTrue();
        result.EffectType.Should().Be("quest_regenerated");
        result.NewQuantity.Should().Be(1);

        // Replay não deve tocar inventário, limite de uso, nem persistir nada de novo.
        _inventoryRepository.Verify(
            r => r.GetByUserIdAndItemKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// US-230 / RN-003: mesmo UseRequestId reenviado com ItemKey diferente → conflito.
    [Fact]
    public async Task UseItem_Throws_WhenUseRequestIdReusedForDifferentItem()
    {
        const string useRequestId = "req-replay-002";
        var existingRequest = ItemUsageRequest.Create(
            UserId, ItemKeys.ReforgeScroll, useRequestId,
            success: true, effectType: "quest_regenerated", message: null,
            remainingQuantity: 1, UtcNow);

        _usageRequestRepository
            .Setup(r => r.GetByUseRequestIdAsync(useRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRequest);

        var act = () => CreateHandler().Handle(
            CreateCommand(itemKey: ItemKeys.FocusPotion, useRequestId: useRequestId), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("USE_REQUEST_ID_MISMATCH");
    }

    // ─── Helper interno ───────────────────────────────────────────────────────

    private sealed class NoConsumeTestHandler(string itemKey) : IItemEffectHandler
    {
        public string ItemKey => itemKey;
        public bool ConsumesOnUse => false;
        public int UsageLimit => 0;
        public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Unlimited;

        public Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
            => Task.FromResult(new ItemEffectResult(true, "no_consume_effect"));
    }
}
