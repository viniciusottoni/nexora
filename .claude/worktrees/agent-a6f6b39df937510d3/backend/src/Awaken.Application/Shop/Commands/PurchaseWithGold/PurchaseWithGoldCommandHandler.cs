using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Shop;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Shop.Commands.PurchaseWithGold;

/// <summary>
/// US-189: orquestrador de compra em Gold.
/// US-190: cada etapa é auditada via IAuditLogService.
/// US-227: débito de Gold, ledger, concessão de item e status final do pedido
/// ocorrem dentro de uma única transação de banco explícita (RN-001/RN-002).
///
/// Fluxo:
///   1. Valida produto ativo com preço em Gold (RN-004).
///   1.1. RN-003: se IdempotencyKey já corresponde a um pedido existente,
///        retorna esse pedido sem novo débito/concessão.
///   2. Cria ShopOrder em "pending" e salva isoladamente (rastreabilidade
///      mesmo que o restante falhe). → audit: ShopOrder.Initiated
///   3. Abre transação de banco explícita.
///   4. Debita GoldWallet via IGoldWalletService (RN-006) dentro da transação.
///   5. Concede item via IInventoryService dentro da mesma transação.
///   6. Marca ShopOrder como "granted" e confirma a transação (commit).
///      → audit: ShopOrder.Granted, disparado só após o commit.
///      Em falha após a abertura da transação → rollback, depois nova
///      operação (fora da transação revertida) marca o pedido como "failed".
///      → audit: ShopOrder.Failed
/// </summary>
public class PurchaseWithGoldCommandHandler(
    IShopProductRepository shopProductRepository,
    IShopOrderRepository shopOrderRepository,
    IGoldWalletService goldWalletService,
    IInventoryService inventoryService,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor,
    IAuditLogService auditLogService,
    ILogger<PurchaseWithGoldCommandHandler> logger)
    : IRequestHandler<PurchaseWithGoldCommand, ShopOrderResponse>
{
    public async Task<ShopOrderResponse> Handle(
        PurchaseWithGoldCommand request, CancellationToken cancellationToken)
    {
        // RN-004: produto deve existir, estar ativo e ter preço em Gold.
        var product = await shopProductRepository.GetByKeyAsync(request.ItemKey, cancellationToken);
        if (product is null || !product.IsActive || !product.PriceGold.HasValue)
            throw new NotFoundException("ShopProduct", request.ItemKey);

        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        // RN-003: reenvio da mesma compra (mesma IdempotencyKey) retorna o
        // pedido já existente, sem novo débito ou concessão.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await shopOrderRepository.GetByExternalTransactionIdAsync(
                request.IdempotencyKey, cancellationToken);
            if (existingOrder is not null)
            {
                logger.LogInformation(
                    "ShopOrder {OrderId} reaproveitado por IdempotencyKey para usuário {UserId} produto {ProductKey}",
                    existingOrder.Id, userId, request.ItemKey);

                return new ShopOrderResponse(
                    existingOrder.Id, existingOrder.Channel, existingOrder.ProductKey,
                    existingOrder.Status, correlationId);
            }
        }

        // RN-002: criar ShopOrder em "pending" antes de qualquer concessão.
        var order = ShopOrder.Create(
            userId, "gold", request.ItemKey,
            externalTransactionId: request.IdempotencyKey, correlationId, utcNow);

        await shopOrderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ShopOrder {OrderId} criado em pending para usuário {UserId} produto {ProductKey} correlationId {CorrelationId}",
            order.Id, userId, request.ItemKey, correlationId);

        // US-190 / RN-001: auditoria de compra iniciada (CA-001).
        // RN-002: metadados sem valor de saldo ou token (ADR-015).
        await TryAuditAsync(
            AuditActions.ShopOrderInitiated,
            userId,
            AuditActorType.User,
            AuditResourceTypes.ShopOrder,
            order.Id,
            AuditMetadata.Safe(new { productKey = request.ItemKey, channel = "gold" }),
            cancellationToken);

        // US-227 / RN-001/RN-002: débito, ledger, concessão de item e status
        // final do pedido ocorrem dentro de uma única transação explícita.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // RN-006: debitar Gold; InsufficientGoldBalanceException se saldo insuficiente.
            await goldWalletService.DebitAsync(
                userId,
                product.PriceGold!.Value,
                reason: "shop_purchase",
                referenceType: "shop_order",
                referenceId: order.Id.ToString(),
                cancellationToken);

            // Conceder item via IInventoryService (US-187).
            await inventoryService.IncrementAsync(userId, request.ItemKey, 1, cancellationToken);

            // RN-002 / CA-001: pedido rastreável com status final.
            order.MarkGranted(dateTimeService.UtcNow);
            shopOrderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // RN-001: só confirma a transação depois que débito, inventário e
            // status do pedido estiverem todos prontos para gravação conjunta.
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "ShopOrder {OrderId} granted para usuário {UserId} produto {ProductKey}",
                order.Id, userId, request.ItemKey);

            // US-190 / RN-001: auditoria de concessão bem-sucedida (CA-001),
            // disparada somente após o commit confirmado.
            await TryAuditAsync(
                AuditActions.ShopOrderGranted,
                userId,
                AuditActorType.User,
                AuditResourceTypes.ShopOrder,
                order.Id,
                AuditMetadata.Safe(new { productKey = request.ItemKey, channel = "gold" }),
                CancellationToken.None);
        }
        catch (InsufficientGoldBalanceException ex)
        {
            // CA-003: saldo insuficiente → rollback (nada foi debitado/concedido,
            // mas a transação pode ter sido aberta) e pedido failed.
            await transaction.RollbackAsync(CancellationToken.None);
            await MarkOrderFailedAsync(order, userId, request.ItemKey, "insufficient_balance");

            logger.LogWarning(
                "ShopOrder {OrderId} failed por saldo insuficiente: usuário {UserId} saldo {Balance} preço {Price}",
                order.Id, userId, ex.CurrentBalance, ex.RequestedAmount);

            // Relança para que o controller retorne 422.
            throw;
        }
        catch (Exception ex)
        {
            // RN-001/RN-002: qualquer falha após a abertura da transação
            // (débito, ledger ou concessão de item) reverte tudo em conjunto —
            // o Gold nunca permanece debitado sem o item concedido.
            await transaction.RollbackAsync(CancellationToken.None);
            await MarkOrderFailedAsync(order, userId, request.ItemKey, "infra_error");

            logger.LogError(ex,
                "ShopOrder {OrderId} failed por erro de infra para usuário {UserId}", order.Id, userId);

            throw;
        }

        return new ShopOrderResponse(
            order.Id, order.Channel, order.ProductKey, order.Status, correlationId);
    }

    /// <summary>
    /// US-227 / RN-007: após o rollback da transação principal, limpa o
    /// tracking pendente, persiste o status "failed" do pedido numa operação
    /// nova e independente e audita a falha.
    /// </summary>
    private async Task MarkOrderFailedAsync(ShopOrder order, Guid userId, string productKey, string reason)
    {
        try
        {
            unitOfWork.ClearChangeTracker();
            order.MarkFailed(dateTimeService.UtcNow);
            shopOrderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception rollbackEx)
        {
            logger.LogError(rollbackEx,
                "Erro ao marcar ShopOrder {OrderId} como failed após rollback", order.Id);
        }

        // US-190 / RN-001: auditoria de falha.
        // RN-002: sem valor de saldo nos metadados (ADR-015).
        await TryAuditAsync(
            AuditActions.ShopOrderFailed,
            userId,
            AuditActorType.User,
            AuditResourceTypes.ShopOrder,
            order.Id,
            AuditMetadata.Safe(new { productKey, channel = "gold", reason }),
            CancellationToken.None);
    }

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
