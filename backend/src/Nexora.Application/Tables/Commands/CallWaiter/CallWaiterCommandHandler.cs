using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.CallWaiter;

/// <summary>US-025, cenários "Chamada direcionada" e "Chamada repetida" — ver docstring de <see cref="WaiterCallCoordinator"/> para o núcleo compartilhado.</summary>
internal sealed class CallWaiterCommandHandler : IRequestHandler<CallWaiterCommand, Result<CallWaiterResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAlertsBroadcaster _alertsBroadcaster;
    private readonly ITableMapBroadcaster _tableMapBroadcaster;

    public CallWaiterCommandHandler(
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

    public async Task<Result<CallWaiterResponse>> Handle(CallWaiterCommand request, CancellationToken cancellationToken)
    {
        var token = request.QrToken.Trim();

        // RN-015: mesma mensagem genérica cobre token inexistente/rotacionado/de outra mesa — nunca
        // revela qual dos casos aconteceu (mesmo padrão de AccessTableByQrTokenCommandHandler).
        var table = string.IsNullOrWhiteSpace(token)
            ? null
            : await _db.DiningTables.SingleOrDefaultAsync(t => t.QrToken == token && t.DeletedAt == null && t.IsActive, cancellationToken);

        if (table is null || table.Id != request.TableId)
        {
            return Result<CallWaiterResponse>.Failure(
                "Não conseguimos reconhecer esta mesa. Chame o garçom para continuar.", ApiErrorCodes.InvalidTableToken);
        }

        var session = await _db.TableSessions.SingleOrDefaultAsync(
            s => s.Id == request.SessionId && s.TableId == table.Id && s.Status != TableSessionStatus.Closed, cancellationToken);

        if (session is null)
        {
            return Result<CallWaiterResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        var outcome = await WaiterCallCoordinator.CallAsync(
            _db, _eventOrigin, _alertsBroadcaster, _tableMapBroadcaster, session, table, cancellationToken);

        return Result<CallWaiterResponse>.Success(new CallWaiterResponse(Acknowledged: true, AlreadyPending: outcome.AlreadyPending));
    }
}
