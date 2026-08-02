using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Queries.GetCurrentSessionBill;

/// <summary>US-027 §10 — ver docstring de <see cref="BillQueryCoordinator"/> e de <see cref="GetCurrentSessionBillQuery"/>.</summary>
internal sealed class GetCurrentSessionBillQueryHandler : IRequestHandler<GetCurrentSessionBillQuery, Result<BillResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetCurrentSessionBillQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<BillResponse>> Handle(GetCurrentSessionBillQuery request, CancellationToken cancellationToken)
    {
        // Nunca 403 (ADR-021/RN-015): ver mesma docstring de GetCurrentSessionConsumptionQueryHandler.
        if (_tenantContext.SessionId is not { } sessionId)
        {
            return Result<BillResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        var session = await _db.TableSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null || session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<BillResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        return await BillQueryCoordinator.BuildAsync(
            _db, session, request.SplitMode, request.People, request.Amount, request.Waived, cancellationToken);
    }
}
