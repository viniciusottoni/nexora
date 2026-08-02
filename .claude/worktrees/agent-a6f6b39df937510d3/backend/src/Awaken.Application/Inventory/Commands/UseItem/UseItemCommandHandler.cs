using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Inventory;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Inventory.Commands.UseItem;

/// <summary>
/// US-230: orquestrador de uso de item do inventário.
///
/// Fluxo:
///   0. RN-003: se UseRequestId já foi processado, retorna a resposta salva
///      sem reaplicar nada (idempotência).
///   1. Busca o InventoryItem e valida existência + quantidade > 0.
///   2. Localiza o IItemEffectHandler pelo ItemKey; usa o handler com ItemKey=="*"
///      (DefaultItemEffectHandler) como fallback se nenhum handler específico
///      estiver registrado.
///   3. Verifica limite de uso por período (se handler.UsageLimit > 0).
///   4. Aplica o efeito via handler.ApplyAsync.
///   5. Se handler.ConsumesOnUse: chama item.ConsumeOne e persiste.
///   6. Registra o uso no ItemUsageRecord para controle de limite.
///   7. Registra o ItemUsageRequest (RN-003) e audita a ação.
///   8. Retorna UseItemResponse.
/// </summary>
public class UseItemCommandHandler(
    IInventoryRepository inventoryRepository,
    IItemUsageRecordRepository usageRecordRepository,
    IItemUsageRequestRepository usageRequestRepository,
    IEnumerable<IItemEffectHandler> effectHandlers,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUserDateService userDateService,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor,
    IAuditLogService auditLogService,
    ILogger<UseItemCommandHandler> logger,
    IServiceProvider serviceProvider)
    : IRequestHandler<UseItemCommand, UseItemResponse>
{
    // ItemKey "*" is the sentinel used by DefaultItemEffectHandler (fallback).
    private const string FallbackHandlerKey = "*";

    public async Task<UseItemResponse> Handle(UseItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        // 0. RN-003: replay seguro — mesmo padrão de PurchaseWithGoldCommandHandler
        // (check-then-act por chave de idempotência, sem transação explícita:
        // este fluxo já é um único SaveChangesAsync atômico).
        var existingRequest = await usageRequestRepository.GetByUseRequestIdAsync(
            request.UseRequestId, cancellationToken);
        if (existingRequest is not null)
        {
            if (existingRequest.ItemKey != request.ItemKey)
                throw new ConflictException(
                    "USE_REQUEST_ID_MISMATCH", "UseRequestId já foi usado para outro item.");

            logger.LogInformation(
                "Item {ItemKey} — UseRequestId {UseRequestId} reaproveitado (replay) para {UserId}",
                request.ItemKey, request.UseRequestId, userId);

            return new UseItemResponse(
                existingRequest.ItemKey,
                existingRequest.Success,
                existingRequest.EffectType,
                existingRequest.RemainingQuantity,
                correlationId);
        }

        // RN-006/P0-3: dia local do usuário — não confundir com UtcNow (hora de
        // servidor). Todo efeito com "dia-alvo" (Tônico/Amuleto) usa este valor.
        var effectiveQuestDateUtc = DateTime.SpecifyKind(
            userDateService.TodayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        // 1. Valida item no inventário.
        var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(userId, request.ItemKey, cancellationToken);
        if (item is null || item.Quantity <= 0)
            throw new NotFoundException("InventoryItem", $"{userId}/{request.ItemKey}");

        // 2. Resolve handler (específico primeiro; fallback pelo key "*").
        var handlerList = effectHandlers.ToList();
        var handler = handlerList.FirstOrDefault(h => h.ItemKey == request.ItemKey)
            ?? handlerList.FirstOrDefault(h => h.ItemKey == FallbackHandlerKey)
            ?? throw new InvalidOperationException($"No effect handler found for item '{request.ItemKey}'.");

        // 3. Verifica limite de uso por período.
        if (handler.UsageLimit > 0)
        {
            var periodStart = GetPeriodStart(handler.LimitPeriod, utcNow);
            var usageRecord = await usageRecordRepository.GetAsync(
                userId, request.ItemKey, periodStart, cancellationToken);

            if (usageRecord is not null && usageRecord.UsageCount >= handler.UsageLimit)
            {
                throw new UsageLimitExceededException(
                    request.ItemKey,
                    handler.UsageLimit,
                    handler.LimitPeriod.ToString().ToLowerInvariant());
            }
        }

        // 4. Aplica o efeito.
        var context = new UseItemContext(
            userId,
            request.ItemKey,
            request.ContextType,
            request.ContextId,
            request.UseRequestId,
            utcNow,
            effectiveQuestDateUtc,
            request.PayloadJson,
            serviceProvider);

        var effectResult = await handler.ApplyAsync(context, cancellationToken);

        // 5. Consome item se necessário.
        if (handler.ConsumesOnUse)
        {
            item.ConsumeOne(utcNow);
            inventoryRepository.Update(item);
        }

        // 6. Registra o uso para controle de limite.
        if (handler.UsageLimit > 0)
        {
            var periodStart = GetPeriodStart(handler.LimitPeriod, utcNow);
            var usageRecord = await usageRecordRepository.GetAsync(
                userId, request.ItemKey, periodStart, cancellationToken);

            if (usageRecord is null)
            {
                var newRecord = ItemUsageRecord.Create(userId, request.ItemKey, periodStart, utcNow);
                await usageRecordRepository.AddAsync(newRecord, cancellationToken);
            }
            else
            {
                usageRecord.IncrementUsage(utcNow);
                usageRecordRepository.Update(usageRecord);
            }
        }

        // 7. RN-003: grava o registro de idempotência no mesmo SaveChanges —
        // não persistir PayloadJson cru (pode conter dado pessoal, ADR-015).
        var usageRequest = ItemUsageRequest.Create(
            userId,
            request.ItemKey,
            request.UseRequestId,
            effectResult.Success,
            effectResult.EffectType,
            message: null,
            remainingQuantity: item.Quantity,
            utcNow);
        await usageRequestRepository.AddAsync(usageRequest, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Item {ItemKey} usado por {UserId} — efeito {EffectType} — nova quantidade {NewQuantity}",
            request.ItemKey, userId, effectResult.EffectType, item.Quantity);

        // Auditoria (não bloqueia o fluxo principal se falhar).
        await TryAuditAsync(
            AuditActions.InventoryItemUsed,
            userId,
            AuditActorType.User,
            AuditResourceTypes.InventoryItem,
            item.Id,
            AuditMetadata.Safe(new { itemKey = request.ItemKey, effectType = effectResult.EffectType }),
            cancellationToken);

        // 8. Retorna resposta.
        return new UseItemResponse(
            request.ItemKey,
            effectResult.Success,
            effectResult.EffectType,
            item.Quantity,
            correlationId);
    }

    private static DateTime GetPeriodStart(ItemUsageLimitPeriod period, DateTime utcNow) =>
        period switch
        {
            ItemUsageLimitPeriod.Daily => utcNow.Date,
            ItemUsageLimitPeriod.Weekly => utcNow.Date.AddDays(-(int)utcNow.DayOfWeek),
            // Unlimited/Lifetime: nunca reseta — hoje nenhum handler combina
            // UsageLimit>0 com Unlimited (só Lifetime, usado pelos Packs, chega
            // aqui de fato); mantido explícito para não regredir se isso mudar.
            ItemUsageLimitPeriod.Unlimited or ItemUsageLimitPeriod.Lifetime => DateTime.MinValue,
            _ => throw new ArgumentOutOfRangeException(
                nameof(period), period, "Período de limite de uso não tratado.")
        };

    private async Task TryAuditAsync(
        string action,
        Guid? actorUserId,
        AuditActorType actorType,
        string resourceType,
        Guid? resourceId,
        string? metadataSafe,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditLogService.RecordAsync(
                action, actorUserId, actorType, resourceType, resourceId, metadataSafe, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Falha ao registrar auditoria: action={Action} resourceType={ResourceType} resourceId={ResourceId}",
                action, resourceType, resourceId);
        }
    }
}
