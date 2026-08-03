using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.UpdateTableSession;

/// <summary>
/// Cenário Gherkin "Troca de garçom responsável" (US-022 §4): "ambos devem constar no histórico
/// da sessão" — <see cref="TableSession"/> guarda só o responsável ATUAL (RN da própria entidade,
/// ver docstring de <see cref="TableSession.ReassignWaiter"/>); o par antes/depois fica em
/// <see cref="AuditLog.Before"/>/<see cref="AuditLog.After"/>, append-only (documento 10), que é
/// exatamente o "histórico" que a US pede.
/// </summary>
internal sealed class UpdateTableSessionCommandHandler : IRequestHandler<UpdateTableSessionCommand, Result<TableSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateTableSessionCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TableSessionResponse>> Handle(UpdateTableSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.Include(s => s.Table)
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<TableSessionResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status == TableSessionStatus.Closed)
        {
            return Result<TableSessionResponse>.Failure(
                "Esta comanda já foi encerrada e não pode mais ser alterada.", ApiErrorCodes.TableSessionNotOpen);
        }

        var now = DateTimeOffset.UtcNow;

        if (request.GuestCount is { } guestCount)
        {
            var previousGuestCount = session.GuestCount;
            session.UpdateGuestCount(guestCount);

            _db.AuditLogs.Add(AuditLog.Create(
                session.TenantId,
                action: "TABLE_SESSION_GUEST_COUNT_UPDATED",
                entity: "table_session",
                occurredAt: now,
                storeId: session.StoreId,
                actorId: _tenantContext.UserId,
                entityId: session.Id,
                before: JsonSerializer.Serialize(new { guestCount = previousGuestCount }),
                after: JsonSerializer.Serialize(new { guestCount })));
        }

        if (request.WaiterId is { } waiterId)
        {
            var previousWaiterId = session.WaiterId;
            session.ReassignWaiter(waiterId);

            // Cenário Gherkin "Troca de garçom responsável": grava os DOIS ids (antes/depois) —
            // é este par que satisfaz "ambos devem constar no histórico da sessão".
            _db.AuditLogs.Add(AuditLog.Create(
                session.TenantId,
                action: "TABLE_SESSION_WAITER_REASSIGNED",
                entity: "table_session",
                occurredAt: now,
                storeId: session.StoreId,
                actorId: _tenantContext.UserId,
                entityId: session.Id,
                before: JsonSerializer.Serialize(new { waiterId = previousWaiterId }),
                after: JsonSerializer.Serialize(new { waiterId })));
        }

        return Result<TableSessionResponse>.Success(TableSessionMapper.Map(session, session.Table));
    }
}
