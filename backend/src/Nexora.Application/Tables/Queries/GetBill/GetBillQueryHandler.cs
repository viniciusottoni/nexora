using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Queries.GetBill;

/// <summary>US-027 §7 — ver docstring de <see cref="BillQueryCoordinator"/> para o núcleo compartilhado com a prévia pública.</summary>
internal sealed class GetBillQueryHandler : IRequestHandler<GetBillQuery, Result<BillResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetBillQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BillResponse>> Handle(GetBillQuery request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result<BillResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<BillResponse>.Failure(
                "Esta comanda já foi encerrada e não permite mais dividir a conta.", ApiErrorCodes.TableSessionNotOpen);
        }

        return await BillQueryCoordinator.BuildAsync(
            _db, session, request.SplitMode, request.People, request.Amount, request.Waived, cancellationToken);
    }
}
