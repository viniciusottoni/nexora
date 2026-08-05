using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Support;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Queries.GetCurrentCashSession;

/// <summary>US-055 §7 — nunca persiste (query, ADR nenhum handler de leitura escreve): o valor esperado é recalculado ao vivo a cada consulta, só é gravado (<see cref="CashSession.SetExpectedAmount"/>) no momento do fechamento.</summary>
internal sealed class GetCurrentCashSessionQueryHandler : IRequestHandler<GetCurrentCashSessionQuery, Result<GetCurrentCashSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetCurrentCashSessionQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GetCurrentCashSessionResponse>> Handle(GetCurrentCashSessionQuery request, CancellationToken cancellationToken)
    {
        var storeId = _tenantContext.StoreId!.Value;
        var operatorId = _tenantContext.UserId!.Value;

        var session = await _db.CashSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.OperatorId == operatorId && s.Status != CashSessionStatus.Closed)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return Result<GetCurrentCashSessionResponse>.Failure(
                "Não há caixa aberto para este operador.", ApiErrorCodes.NoOpenCashSession);
        }

        var expected = await CashExpectedAmountCalculator.CalculateAsync(_db, session.Id, session.OpeningAmount, cancellationToken);

        return Result<GetCurrentCashSessionResponse>.Success(
            new GetCurrentCashSessionResponse(CashSessionMapper.Map(session), expected));
    }
}
