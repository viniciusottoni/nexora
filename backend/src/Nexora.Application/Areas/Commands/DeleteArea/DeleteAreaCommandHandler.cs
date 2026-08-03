using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Areas.Commands.DeleteArea;

/// <summary>
/// Exclusão física nunca existe (CLAUDE.md, "Soft delete sempre") — marca <c>deleted_at</c>.
/// Recusa se houver qualquer mesa não excluída no ambiente: o gestor precisa mover/excluir as
/// mesas primeiro (mesma lógica de "recusa por histórico" da US-020, aplicada uma camada acima —
/// aqui não é histórico de sessão, é a própria existência de mesas cadastradas no ambiente).
/// </summary>
internal sealed class DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeleteAreaCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(DeleteAreaCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var area = await _db.Areas.SingleOrDefaultAsync(
            a => a.Id == request.Id && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
        if (area is null)
        {
            return Result.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
        }

        var hasTables = await _db.DiningTables.AnyAsync(t => t.AreaId == area.Id && t.DeletedAt == null, cancellationToken);
        if (hasTables)
        {
            return Result.Failure(
                "Este ambiente possui mesas cadastradas. Mova ou exclua as mesas antes de excluir o ambiente.",
                ApiErrorCodes.AreaHasActiveTables);
        }

        area.SoftDelete();

        _db.AuditLogs.Add(AuditLog.Create(
            area.TenantId,
            action: "AREA_DELETED",
            entity: "area",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: area.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: area.Id));

        return Result.Success();
    }
}
