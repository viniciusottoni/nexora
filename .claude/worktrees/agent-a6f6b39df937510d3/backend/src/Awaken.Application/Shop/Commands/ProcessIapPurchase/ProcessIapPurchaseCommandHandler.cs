using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Shop.Commands.CreditGoldFromPurchase;
using Awaken.Contracts.Shop;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Shop.Commands.ProcessIapPurchase;

/// <summary>
/// US-189 (refatoração): compra IAP agora cria ShopOrder (canal "iap") como
/// rastreamento unificado (CA-001), mantendo o IapTransactionLedger existente
/// para retrocompatibilidade.
/// US-190: cada etapa é auditada via IAuditLogService.
/// US-195: server-side transaction validation via RevenueCat before granting items.
/// US-226: produtos com ShopProduct.GoldAmount creditam Gold na carteira (em vez
/// de inventário), sempre por quantidade resolvida no servidor (RN-001/RN-002).
///
/// Idempotência (ADR-010 / CA-002):
///   • Se ShopOrder com ExternalTransactionId já existir em "granted" → retorna sem conceder novamente.
///   • Se IapTransactionLedger já existir em "granted" sem ShopOrder → cria ShopOrder e retorna.
///
/// Validation flow (US-195 / US-226):
///   • Validate transaction with RevenueCat before granting any item.
///   • If validation fails: mark order "rejected", audit, return without granting.
///   • RN-005: se a transação validada pertence a outro usuário (AppUserId
///     divergente de request.UserId), trata como falha de validação.
///   • If validation succeeds: proceed with existing grant flow.
/// </summary>
public class ProcessIapPurchaseCommandHandler(
    IIapTransactionLedgerRepository ledgerRepository,
    IShopProductRepository shopProductRepository,
    IInventoryRepository inventoryRepository,
    IShopOrderRepository shopOrderRepository,
    IRevenueCatValidationService revenueCatValidationService,
    ISender sender,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor,
    IAuditLogService auditLogService,
    ILogger<ProcessIapPurchaseCommandHandler> logger) : IRequestHandler<ProcessIapPurchaseCommand, ShopOrderResponse>
{
    public async Task<ShopOrderResponse> Handle(ProcessIapPurchaseCommand request, CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        // CA-002 / ADR-010: idempotência por ExternalTransactionId no ShopOrder.
        var existingOrder = await shopOrderRepository.GetByExternalTransactionIdAsync(
            request.TransactionId, cancellationToken);

        if (existingOrder is not null && existingOrder.Status == "granted")
        {
            logger.LogInformation(
                "ShopOrder para IAP transaction {TransactionId} já concedido (OrderId={OrderId}). Ignorando.",
                request.TransactionId, existingOrder.Id);
            return ToResponse(existingOrder, correlationId);
        }

        // Compatibilidade: verificar também o ledger legado.
        var existing = await ledgerRepository.GetByTransactionIdAsync(request.TransactionId, cancellationToken);
        if (existing is not null && existing.Status == "granted")
        {
            logger.LogInformation(
                "IAP transaction {TransactionId} já concedida no ledger para usuário {UserId}. Ignorando.",
                request.TransactionId, request.UserId);

            if (existingOrder is not null)
                return ToResponse(existingOrder, correlationId);

            // Compatibilidade: ledger legado já "granted" sem ShopOrder correspondente
            // (dados anteriores à introdução de ShopOrder). Cria o registro rastreável
            // já em "granted" para manter a resposta consistente, sem repetir a concessão.
            var legacyOrder = ShopOrder.Create(
                request.UserId, "iap", request.ProductKey,
                request.TransactionId, correlationId, dateTimeService.UtcNow);
            legacyOrder.MarkGranted(dateTimeService.UtcNow);
            await shopOrderRepository.AddAsync(legacyOrder, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ToResponse(legacyOrder, correlationId);
        }

        var utcNow = dateTimeService.UtcNow;

        // Criar ou recuperar entrada no ledger legado.
        IapTransactionLedger ledger;
        if (existing is null)
        {
            ledger = IapTransactionLedger.Create(
                request.UserId, request.TransactionId, request.ProductKey, request.Store, utcNow);
            await ledgerRepository.AddAsync(ledger, cancellationToken);
        }
        else
        {
            ledger = existing;
        }

        // RN-002: criar ShopOrder em "pending" se ainda não existir.
        ShopOrder order;
        if (existingOrder is null)
        {
            order = ShopOrder.Create(
                request.UserId, "iap", request.ProductKey,
                request.TransactionId, correlationId, utcNow);
            await shopOrderRepository.AddAsync(order, cancellationToken);
        }
        else
        {
            order = existingOrder;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // US-190 / RN-001: auditoria de compra IAP iniciada (CA-001).
        // RN-003: concessões via webhook usam ActorType = System.
        // RN-002: metadados sem token ou dado de pagamento (ADR-015 / CA-002).
        await TryAuditAsync(
            AuditActions.ShopOrderInitiated,
            null,
            AuditActorType.System,
            AuditResourceTypes.ShopOrder,
            order.Id,
            AuditMetadata.Safe(new { productKey = request.ProductKey, channel = "iap" }),
            cancellationToken);

        // US-195 / US-226: server-side validation via RevenueCat.
        // Items/Gold must only be granted after the transaction is confirmed server-side.
        RevenueCatTransactionValidation validation;
        try
        {
            // O app faz Purchases.logIn com o UserId do backend, então o
            // subscriber no RevenueCat é consultável por esse mesmo ID.
            validation = await revenueCatValidationService.ValidateTransactionAsync(
                request.TransactionId, request.UserId.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            // US-226 / fluxo alternativo "Provider indisponível": falha temporária
            // do provider não credita Gold nem item — pedido permanece "pending"
            // para nova tentativa (não falha definitivamente).
            logger.LogError(ex,
                "Falha temporária ao validar transação IAP {TransactionId} no provider. Pedido permanece pending.",
                request.TransactionId);

            await TryAuditAsync(
                AuditActions.ShopOrderFailed,
                null,
                AuditActorType.System,
                AuditResourceTypes.ShopOrder,
                order.Id,
                AuditMetadata.Safe(new { productKey = request.ProductKey, channel = "iap", reason = "provider_unavailable" }),
                CancellationToken.None);

            return ToResponse(order, correlationId);
        }

        // RN-005: transação validada para outro usuário não pode creditar a carteira atual.
        var userMismatch = !string.IsNullOrEmpty(validation.AppUserId)
            && !string.Equals(validation.AppUserId, request.UserId.ToString(), StringComparison.OrdinalIgnoreCase);

        if (!validation.IsValid || userMismatch)
        {
            ledger.MarkFailed(utcNow);
            order.MarkFailed(utcNow);

            if (existing is not null)
                ledgerRepository.Update(ledger);

            shopOrderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var reason = userMismatch ? "user_mismatch" : "transaction_validation_failed";

            logger.LogWarning(
                "IAP transaction {TransactionId} rejected: reason={Reason} for userId={UserId} productKey={ProductKey}",
                request.TransactionId, reason, request.UserId, request.ProductKey);

            // US-195 / US-226 / RN-001: auditoria de rejeição por validação.
            // RN-008: nenhum payload sensível do provider é incluído nos metadados.
            await TryAuditAsync(
                AuditActions.ShopOrderFailed,
                null,
                AuditActorType.System,
                AuditResourceTypes.ShopOrder,
                order.Id,
                AuditMetadata.Safe(new { productKey = request.ProductKey, channel = "iap", reason }),
                CancellationToken.None);

            // Return silently — the item is not granted. The webhook will confirm if valid.
            return ToResponse(order, correlationId);
        }

        // RN-004/RN-006: produto deve existir e estar ativo.
        var product = await shopProductRepository.GetByKeyAsync(request.ProductKey, cancellationToken);
        if (product is null || !product.IsActive)
        {
            ledger.MarkFailed(utcNow);
            order.MarkFailed(utcNow);
            ledgerRepository.Update(ledger);
            shopOrderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // US-190 / RN-001: auditoria de falha por produto não encontrado/inativo.
            await TryAuditAsync(
                AuditActions.ShopOrderFailed,
                null,
                AuditActorType.System,
                AuditResourceTypes.ShopOrder,
                order.Id,
                AuditMetadata.Safe(new
                {
                    productKey = request.ProductKey,
                    channel = "iap",
                    reason = product is null ? "product_not_found" : "product_inactive",
                }),
                CancellationToken.None);

            if (product is null)
                throw new NotFoundException("ShopProduct", request.ProductKey);

            // Produto inativo: bloqueia crédito (RN-006) sem lançar exceção —
            // o app recebe o status "failed" de forma controlada.
            return ToResponse(order, correlationId);
        }

        // RN-001/RN-002/RN-006: produto de pacote de Gold credita carteira; quantidade
        // vem exclusivamente de ShopProduct.GoldAmount, nunca do payload do app.
        if (product.GoldAmount.HasValue)
        {
            if (product.GoldAmount.Value <= 0)
            {
                // RN-006: produto divergente (GoldAmount configurado incorretamente)
                // bloqueia o crédito.
                ledger.MarkFailed(utcNow);
                order.MarkFailed(utcNow);
                ledgerRepository.Update(ledger);
                shopOrderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogWarning(
                    "ShopOrder {OrderId} failed: produto {ProductKey} com GoldAmount inválido ({GoldAmount}).",
                    order.Id, request.ProductKey, product.GoldAmount.Value);

                await TryAuditAsync(
                    AuditActions.ShopOrderFailed,
                    null,
                    AuditActorType.System,
                    AuditResourceTypes.ShopOrder,
                    order.Id,
                    AuditMetadata.Safe(new { productKey = request.ProductKey, channel = "iap", reason = "product_mismatch" }),
                    CancellationToken.None);

                return ToResponse(order, correlationId);
            }

            // US-226 seção 9: comando dedicado para crédito de Gold, separado da
            // concessão de itens de inventário (testável isoladamente).
            await sender.Send(
                new CreditGoldFromPurchaseCommand(request.UserId, product.GoldAmount.Value, order.Id),
                cancellationToken);
        }
        else if (product.Type == "consumable")
        {
            var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
                request.UserId, request.ProductKey, cancellationToken);

            var maxQuantity = ItemCatalog.Find(request.ProductKey)?.MaxQuantity ?? 99;
            if ((item?.Quantity ?? 0) + 1 > maxQuantity)
                throw new ConflictException(
                    "INVENTORY_MAX_QUANTITY",
                    $"Quantidade maxima de {maxQuantity} atingida para o item '{request.ProductKey}'.");

            if (item is null)
            {
                item = InventoryItem.Create(request.UserId, request.ProductKey);
                item.Add(1, utcNow);
                await inventoryRepository.AddAsync(item, cancellationToken);
            }
            else
            {
                item.Add(1, utcNow);
                inventoryRepository.Update(item);
            }
        }
        // "slot" type — downstream features check granted ledger/ShopOrder records.

        ledger.MarkGranted(utcNow);
        order.MarkGranted(utcNow);

        if (existing is not null)
            ledgerRepository.Update(ledger);

        shopOrderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "IAP transaction {TransactionId} granted para usuário {UserId} produto {ProductKey} OrderId {OrderId}",
            request.TransactionId, request.UserId, request.ProductKey, order.Id);

        // US-190 / RN-001: auditoria de concessão IAP bem-sucedida (CA-001).
        await TryAuditAsync(
            AuditActions.ShopOrderGranted,
            null,
            AuditActorType.System,
            AuditResourceTypes.ShopOrder,
            order.Id,
            AuditMetadata.Safe(new { productKey = request.ProductKey, channel = "iap" }),
            CancellationToken.None);

        return ToResponse(order, correlationId);
    }

    private static ShopOrderResponse ToResponse(ShopOrder order, string? correlationId) =>
        new(order.Id, order.Channel, order.ProductKey, order.Status, correlationId ?? order.CorrelationId);

    /// <summary>
    /// US-190 / RN-005: falha ao auditar deve ser observável (warning), mas não
    /// pode cancelar a transação principal.
    /// </summary>
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
            // RN-005: observável, mas não bloqueia a transação.
            logger.LogWarning(ex,
                "Falha ao registrar auditoria: action={Action} resourceType={ResourceType} resourceId={ResourceId}",
                action, resourceType, resourceId);
        }
    }
}
