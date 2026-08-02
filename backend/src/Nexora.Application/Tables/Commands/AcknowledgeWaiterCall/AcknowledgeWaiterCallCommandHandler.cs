using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Operation;
using Nexora.Domain.Metrics;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.AcknowledgeWaiterCall;

/// <summary>US-025, cenário "Confirmação de atendimento".</summary>
internal sealed class AcknowledgeWaiterCallCommandHandler : IRequestHandler<AcknowledgeWaiterCallCommand, Result<AcknowledgeWaiterCallResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ITableMapBroadcaster _tableMapBroadcaster;

    public AcknowledgeWaiterCallCommandHandler(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, ITableMapBroadcaster tableMapBroadcaster)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tableMapBroadcaster = tableMapBroadcaster;
    }

    public async Task<Result<AcknowledgeWaiterCallResponse>> Handle(AcknowledgeWaiterCallCommand request, CancellationToken cancellationToken)
    {
        var table = await _db.DiningTables.SingleOrDefaultAsync(
            t => t.Id == request.TableId && t.DeletedAt == null, cancellationToken);
        if (table is null)
        {
            return Result<AcknowledgeWaiterCallResponse>.Failure("Mesa não encontrada.", ApiErrorCodes.TableNotFound);
        }

        // "Sessão corrente" = a mais recente ainda não liberada — mesmo critério de GetTableMapQueryHandler.
        var session = await _db.TableSessions
            .Where(s => s.TableId == table.Id && s.ReleasedAt == null)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return Result<AcknowledgeWaiterCallResponse>.Failure("Não há comanda aberta para esta mesa.", ApiErrorCodes.TableSessionNotFound);
        }

        var alert = await _db.Alerts.SingleOrDefaultAsync(
            a => a.TenantId == session.TenantId
                 && a.EntityType == "table_session"
                 && a.EntityId == session.Id
                 && a.Type == AlertTypes.WaiterCalled
                 && a.ResolvedAt == null,
            cancellationToken);

        if (alert is null)
        {
            return Result<AcknowledgeWaiterCallResponse>.Failure(
                "Não há chamada de garçom pendente para esta mesa.", ApiErrorCodes.NoPendingWaiterCall);
        }

        var now = DateTimeOffset.UtcNow;
        var responseSeconds = (int)Math.Max(0, (now - alert.RaisedAt).TotalSeconds);

        if (_tenantContext.UserId is { } userId)
        {
            alert.Acknowledge(userId);
        }

        alert.Resolve();

        // US-025 §4 "Confirmação de atendimento": "o indicador deve desaparecer do mapa" — o
        // próximo GET /v1/tables já reflete o Alert resolvido (GetTableMapQueryHandler), mas o
        // broadcast avisa quem já está com o mapa aberto sem precisar recarregar.
        await _tableMapBroadcaster.NotifySignalAsync(
            session.TenantId, "table.waiter_call_acknowledged", new { tableId = table.Id }, cancellationToken);

        return Result<AcknowledgeWaiterCallResponse>.Success(new AcknowledgeWaiterCallResponse(Resolved: true, ResponseSeconds: responseSeconds));
    }
}
