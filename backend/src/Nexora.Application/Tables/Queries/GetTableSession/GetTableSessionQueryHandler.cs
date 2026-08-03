using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Queries.GetTableSession;

internal sealed class GetTableSessionQueryHandler : IRequestHandler<GetTableSessionQuery, Result<TableSessionResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTableSessionQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TableSessionResponse>> Handle(GetTableSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions
            .Include(s => s.Table)
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result<TableSessionResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        return Result<TableSessionResponse>.Success(TableSessionMapper.Map(session, session.Table));
    }
}
