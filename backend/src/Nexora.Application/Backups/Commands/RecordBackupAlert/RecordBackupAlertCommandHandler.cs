using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Backups.Commands.RecordBackupAlert;

internal sealed class RecordBackupAlertCommandHandler : IRequestHandler<RecordBackupAlertCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public RecordBackupAlertCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public Task<Result> Handle(RecordBackupAlertCommand request, CancellationToken cancellationToken)
    {
        if (request.InstallationId != request.AuthenticatedInstallationId)
        {
            return Task.FromResult(Result.Failure("Instalação não autorizada.", ApiErrorCodes.BackupPermissionDenied));
        }

        if (_tenantContext.TenantId is null || _tenantContext.StoreId is null)
        {
            return Task.FromResult(Result.Failure(
                "Não foi possível identificar a loja vinculada a esta instalação.",
                ApiErrorCodes.TenantContextMissing));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            _tenantContext.TenantId.Value,
            action: "EDGE_BACKUP_UPLOAD_FAILED",
            entity: "edge_installation",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: _tenantContext.StoreId.Value,
            entityId: request.InstallationId,
            after: JsonSerializer.Serialize(new { reason = request.Reason, occurredAt = request.OccurredAt })));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Task.FromResult(Result.Success());
    }
}
