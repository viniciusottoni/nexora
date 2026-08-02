using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Common;
using Awaken.Contracts.Shop;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Awaken.Application.Shop.Queries.GetAdminShopOrders;

/// <summary>
/// US-193: handler de consulta administrativa de pedidos.
///
/// RN-001: acesso protegido por RBAC (validado no controller via [Authorize(Policy="Admin")]).
/// RN-002: somente leitura — nenhuma mutação ocorre aqui.
/// RN-003: projeção segura — sem token/dado de pagamento; ExternalTransactionId é ID de referência.
/// RN-004 / CA-003: cada consulta gera AuditLog com ActorType = Admin.
/// RN-005: CorrelationId presente na projeção para cruzamento com auditoria.
/// </summary>
public class GetAdminShopOrdersQueryHandler(
    IShopOrderRepository shopOrderRepository,
    ICurrentUserService currentUserService,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetAdminShopOrdersQuery, PagedResponse<AdminShopOrderResponse>>
{
    public async Task<PagedResponse<AdminShopOrderResponse>> Handle(
        GetAdminShopOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await shopOrderRepository.GetPagedByFilterAsync(
            request.UserId,
            request.Status,
            request.Channel,
            request.DateFrom,
            request.DateTo,
            request.Page,
            request.PageSize,
            cancellationToken);

        var projected = items
            .Select(o => new AdminShopOrderResponse(
                o.Id,
                o.UserId,
                o.Channel,
                o.ProductKey,
                o.Status,
                o.ExternalTransactionId, // ID de referência, não dado de pagamento (RN-003)
                o.CorrelationId,
                o.CreatedAtUtc,
                o.UpdatedAtUtc,
                o.GrantedAtUtc,
                o.FailedAtUtc))
            .ToList();

        // RN-004 / CA-003: auditoria do acesso administrativo.
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
        var adminUserId = currentUserService.UserId;
        var metadataSafe = $"{{\"filters\":{{\"userId\":\"{request.UserId}\",\"status\":\"{request.Status}\",\"channel\":\"{request.Channel}\"}},\"page\":{request.Page},\"pageSize\":{request.PageSize},\"totalCount\":{totalCount}}}";

        try
        {
            await auditLogService.RecordAsync(
                AuditActions.AdminShopOrdersQueried,
                adminUserId,
                AuditActorType.Admin,
                AuditResourceTypes.ShopOrder,
                resourceId: null,
                metadataSafe: metadataSafe,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Falha de auditoria não cancela a consulta (princípio de resiliência — US-190 RN-005).
        }

        var hasMore = (request.Page * request.PageSize) < totalCount;

        return new PagedResponse<AdminShopOrderResponse>(
            projected,
            request.Page,
            request.PageSize,
            totalCount,
            hasMore);
    }
}
