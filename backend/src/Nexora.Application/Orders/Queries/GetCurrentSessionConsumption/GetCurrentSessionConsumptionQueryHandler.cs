using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Queries.GetCurrentSessionConsumption;

/// <summary>
/// US-024 (Consumo da mesa em tempo real) — lista os itens já lançados na sessão corrente com
/// quantidade/valor/status traduzido, subtotal, taxa de serviço estimada (RN-010,
/// <see cref="ServiceFeePolicy"/>) e total. Item cancelado aparece (riscado no frontend via
/// <c>Cancelled</c>) mas NÃO compõe subtotal/total (cenário Gherkin "Item cancelado").
/// </summary>
internal sealed class GetCurrentSessionConsumptionQueryHandler
    : IRequestHandler<GetCurrentSessionConsumptionQuery, Result<SessionConsumptionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetCurrentSessionConsumptionQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SessionConsumptionResponse>> Handle(GetCurrentSessionConsumptionQuery request, CancellationToken cancellationToken)
    {
        // Nunca 403 aqui (ADR-021/RN-015): sem claim "ses" válida o esquema "TableSession" já teria
        // recusado a autenticação (401) antes deste handler ser alcançado; a ausência defensiva
        // abaixo cai no MESMO código de "sessão não encontrada" que qualquer outro caso de sessão
        // inexistente, nunca em "acesso negado".
        if (_tenantContext.SessionId is not { } sessionId)
        {
            return Result<SessionConsumptionResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        var session = await _db.TableSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null || session.Status == TableSessionStatus.Closed)
        {
            return Result<SessionConsumptionResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        var items = await _db.OrderItems
            .AsNoTracking()
            .Where(i => _db.Orders.Any(o => o.Id == i.OrderId && o.SessionId == session.Id))
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .OrderBy(i => i.PlacedAt)
            .ToListAsync(cancellationToken);

        var tenantConfig = await _db.TenantConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == session.TenantId, cancellationToken);
        var feePercent = ServiceFeePolicy.ResolvePercent(tenantConfig?.Operation);

        var now = DateTimeOffset.UtcNow;

        // Item cancelado é exibido (frontend risca) mas nunca compõe subtotal/total.
        var subtotal = items.Where(i => i.Status != OrderItemStatus.Cancelled).Sum(i => i.TotalPrice);
        var serviceFee = ServiceFeePolicy.CalculateFee(subtotal, feePercent);
        var total = subtotal + serviceFee;

        var itemResponses = items.Select(item =>
        {
            var stillCooking = item.Status is OrderItemStatus.Queued or OrderItemStatus.Fired or OrderItemStatus.InOven or OrderItemStatus.OutOfOven;
            int? etaMinutes = stillCooking
                ? Math.Max(0, item.Variant.PrepMinutes - (int)Math.Floor((now - item.PlacedAt).TotalMinutes))
                : null;

            return new SessionConsumptionItemResponse(
                item.Id,
                item.OrderId,
                $"{item.Variant.Product.Name} {item.Variant.Name}".Trim(),
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice,
                OrderItemStatusLabels.ToWireStatus(item.Status),
                OrderItemStatusLabels.ClientLabel(item.Status),
                etaMinutes,
                item.Status == OrderItemStatus.Cancelled,
                item.VariantId,
                item.Variant.Product.IsActive && item.Variant.Product.IsAvailable);
        }).ToList();

        var minutesOpen = Math.Max(0, (int)Math.Floor((now - session.OpenedAt).TotalMinutes));

        return Result<SessionConsumptionResponse>.Success(new SessionConsumptionResponse(
            itemResponses,
            subtotal,
            serviceFee,
            ServiceFeeOptional: true,
            total,
            session.OpenedAt,
            minutesOpen));
    }
}
