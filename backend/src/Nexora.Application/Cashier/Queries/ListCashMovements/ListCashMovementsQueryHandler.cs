using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Support;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Queries.ListCashMovements;

internal sealed class ListCashMovementsQueryHandler : IRequestHandler<ListCashMovementsQuery, Result<ListCashMovementsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListCashMovementsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ListCashMovementsResponse>> Handle(ListCashMovementsQuery request, CancellationToken cancellationToken)
    {
        var storeId = _tenantContext.StoreId!.Value;
        var operatorId = _tenantContext.UserId!.Value;

        var session = await _db.CashSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.OperatorId == operatorId && s.Status != CashSessionStatus.Closed)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return Result<ListCashMovementsResponse>.Failure("Não há caixa aberto para este operador.", ApiErrorCodes.NoOpenCashSession);
        }

        var movements = await _db.CashMovements.AsNoTracking()
            .Where(m => m.CashSessionId == session.Id)
            .OrderByDescending(m => m.OccurredAt)
            .ToListAsync(cancellationToken);

        return Result<ListCashMovementsResponse>.Success(
            new ListCashMovementsResponse(movements.Select(CashSessionMapper.Map).ToList()));
    }
}
