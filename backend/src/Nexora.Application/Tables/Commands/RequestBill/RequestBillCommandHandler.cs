using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.RequestBill;

/// <summary>US-026, cenário "Solicitação pelo garçom" — ver docstring de <see cref="BillRequestCoordinator"/> para o núcleo compartilhado.</summary>
internal sealed class RequestBillCommandHandler : IRequestHandler<RequestBillCommand, Result<RequestBillResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAlertsBroadcaster _alertsBroadcaster;
    private readonly ITableMapBroadcaster _tableMapBroadcaster;

    public RequestBillCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IAlertsBroadcaster alertsBroadcaster,
        ITableMapBroadcaster tableMapBroadcaster)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _alertsBroadcaster = alertsBroadcaster;
        _tableMapBroadcaster = tableMapBroadcaster;
    }

    public async Task<Result<RequestBillResponse>> Handle(RequestBillCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.Include(s => s.Table)
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result<RequestBillResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        var outcome = await BillRequestCoordinator.RequestAsync(
            _db, _eventOrigin, _alertsBroadcaster, _tableMapBroadcaster, session, session.Table, request.SplitMode, request.People, cancellationToken);

        if (outcome.IsFailure)
        {
            return Result<RequestBillResponse>.Failure(outcome.Error!, outcome.Code, outcome.Errors);
        }

        return Result<RequestBillResponse>.Success(new RequestBillResponse(outcome.Value!.Session, outcome.Value.AlreadyRequested));
    }
}
