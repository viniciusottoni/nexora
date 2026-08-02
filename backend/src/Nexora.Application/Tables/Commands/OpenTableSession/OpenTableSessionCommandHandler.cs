using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.OpenTableSession;

/// <summary>
/// Cenários Gherkin "Abertura pelo garçom" e "Mesa já ocupada" (US-022 §4). A checagem de sessão
/// única e a gravação de estado+evento vivem em <see cref="TableSessionOpener"/>, reaproveitado
/// por <see cref="Commands.AccessTableByQrToken.AccessTableByQrTokenCommandHandler"/> (US-021).
/// </summary>
internal sealed class OpenTableSessionCommandHandler : IRequestHandler<OpenTableSessionCommand, Result<TableSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ICurrentTenantContext _tenantContext;

    public OpenTableSessionCommandHandler(IApplicationDbContext db, IEventOriginProvider eventOrigin, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TableSessionResponse>> Handle(OpenTableSessionCommand request, CancellationToken cancellationToken)
    {
        var table = await _db.DiningTables.SingleOrDefaultAsync(
            t => t.Id == request.TableId && t.DeletedAt == null, cancellationToken);
        if (table is null)
        {
            return Result<TableSessionResponse>.Failure("Mesa não encontrada.", ApiErrorCodes.TableNotFound);
        }

        // RN-004 "toda ação registra autor" — o garçom autenticado é, ao mesmo tempo, quem abriu
        // (opened_by, auditoria) e o responsável inicial pela mesa (waiter_id).
        var waiterId = _tenantContext.UserId;

        var opened = await TableSessionOpener.OpenAsync(
            _db,
            _eventOrigin,
            table,
            requestedGuestCount: request.GuestCount,
            waiterId: waiterId,
            openedBy: waiterId,
            source: "WAITER",
            occurredAt: request.OccurredAt,
            cancellationToken);

        if (opened.IsFailure)
        {
            return Result<TableSessionResponse>.Failure(opened.Error!, opened.Code, opened.Errors);
        }

        return Result<TableSessionResponse>.Success(TableSessionMapper.Map(opened.Value!, table));
    }
}
