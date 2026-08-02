using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.DeleteTable;

/// <summary>
/// Cenário Gherkin "Exclusão de mesa com histórico": mesa com sessão encerrada no histórico não
/// pode ser excluída fisicamente (nunca existe exclusão física, CLAUDE.md "Soft delete sempre") —
/// a exclusão é recusada e a desativação (<see cref="Nexora.Application.Tables.Commands.SetTableActive.SetTableActiveCommand"/>)
/// é a alternativa oferecida ao gestor. Sem histórico, a mesa pode ser excluída (soft delete)
/// normalmente.
/// </summary>
internal sealed class DeleteTableCommandHandler : IRequestHandler<DeleteTableCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeleteTableCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(DeleteTableCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var table = await _db.DiningTables.SingleOrDefaultAsync(
            t => t.Id == request.TableId && t.TenantId == tenantId && t.DeletedAt == null, cancellationToken);
        if (table is null)
        {
            return Result.Failure("Mesa não encontrada.", ApiErrorCodes.TableNotFound);
        }

        var hasHistory = await _db.TableSessions.AnyAsync(s => s.TableId == table.Id, cancellationToken);
        if (hasHistory)
        {
            return Result.Failure(
                "Esta mesa tem sessões no histórico e não pode ser excluída. Desative-a para removê-la do salão sem perder o histórico.",
                ApiErrorCodes.TableHasSessionHistory);
        }

        table.SoftDelete();

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "TABLE_DELETED",
            entity: "dining_table",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: table.Id));

        return Result.Success();
    }
}
