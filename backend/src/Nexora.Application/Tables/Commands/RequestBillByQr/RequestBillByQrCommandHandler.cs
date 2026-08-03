using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.RequestBillByQr;

/// <summary>
/// US-026, cenário "Solicitação pelo cliente" — ver docstring de <see cref="BillRequestCoordinator"/>
/// para o núcleo compartilhado (inclui a checagem de itens pendentes da US-035). O cliente nunca
/// autoriza um fechamento bloqueado (não há diálogo de PIN no QR) — <c>authorizationToken</c> e
/// <c>reason</c> chegam sempre nulos ao coordenador por este caminho, propositalmente.
/// </summary>
internal sealed class RequestBillByQrCommandHandler : IRequestHandler<RequestBillByQrCommand, Result<RequestBillResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAlertsBroadcaster _alertsBroadcaster;
    private readonly ITableMapBroadcaster _tableMapBroadcaster;
    private readonly IAuthorizationTokenValidator _authorizationValidator;

    public RequestBillByQrCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IAlertsBroadcaster alertsBroadcaster,
        ITableMapBroadcaster tableMapBroadcaster,
        IAuthorizationTokenValidator authorizationValidator)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _alertsBroadcaster = alertsBroadcaster;
        _tableMapBroadcaster = tableMapBroadcaster;
        _authorizationValidator = authorizationValidator;
    }

    public async Task<Result<RequestBillResponse>> Handle(RequestBillByQrCommand request, CancellationToken cancellationToken)
    {
        var token = request.QrToken.Trim();

        var table = string.IsNullOrWhiteSpace(token)
            ? null
            : await _db.DiningTables.SingleOrDefaultAsync(t => t.QrToken == token && t.DeletedAt == null && t.IsActive, cancellationToken);

        if (table is null || table.Id != request.TableId)
        {
            return Result<RequestBillResponse>.Failure(
                "Não conseguimos reconhecer esta mesa. Chame o garçom para continuar.", ApiErrorCodes.InvalidTableToken);
        }

        var session = await _db.TableSessions.SingleOrDefaultAsync(
            s => s.Id == request.SessionId && s.TableId == table.Id && s.Status != TableSessionStatus.Closed, cancellationToken);

        if (session is null)
        {
            return Result<RequestBillResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        var outcome = await BillRequestCoordinator.RequestAsync(
            _db, _eventOrigin, _alertsBroadcaster, _tableMapBroadcaster, _authorizationValidator,
            session, table, request.SplitMode, request.People, authorizationToken: null, reason: null, cancellationToken);

        if (outcome.IsFailure)
        {
            return Result<RequestBillResponse>.Failure(outcome.Error!, outcome.Code, outcome.Errors);
        }

        return Result<RequestBillResponse>.Success(
            new RequestBillResponse(outcome.Value!.Session, outcome.Value.AlreadyRequested, outcome.Value.PendingItems));
    }
}
