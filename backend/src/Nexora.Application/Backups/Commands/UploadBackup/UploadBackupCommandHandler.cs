using System.Security.Cryptography;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Abstractions.Storage;
using Nexora.Contracts.Backups;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Backups.Commands.UploadBackup;

internal sealed class UploadBackupCommandHandler : IRequestHandler<UploadBackupCommand, Result<UploadBackupResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IBackupStorage _storage;

    public UploadBackupCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IBackupStorage storage)
    {
        _db = db;
        _tenantContext = tenantContext;
        _storage = storage;
    }

    public async Task<Result<UploadBackupResponse>> Handle(UploadBackupCommand request, CancellationToken cancellationToken)
    {
        if (request.InstallationId != request.AuthenticatedInstallationId)
        {
            return Result<UploadBackupResponse>.Failure("Instalação não autorizada.", ApiErrorCodes.BackupPermissionDenied);
        }

        if (_tenantContext.TenantId is null || _tenantContext.StoreId is null)
        {
            return Result<UploadBackupResponse>.Failure(
                "Não foi possível identificar a loja vinculada a esta instalação.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;

        var calculated = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
        if (!string.Equals(calculated, request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            return Result<UploadBackupResponse>.Failure("Hash do backup não confere.", ApiErrorCodes.BackupHashMismatch);
        }

        var backupClass = request.BackupClass == "daily" ? BackupClass.Daily : BackupClass.SixHour;

        var stored = await _storage.PutAsync(
            new BackupPutRequest(tenantId, request.InstallationId, backupClass, request.Content),
            cancellationToken);

        if (!string.Equals(stored.Sha256, request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            return Result<UploadBackupResponse>.Failure("Hash do backup não confere.", ApiErrorCodes.BackupHashMismatch);
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "EDGE_BACKUP_UPLOADED",
            entity: "edge_installation",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: storeId,
            entityId: request.InstallationId,
            after: JsonSerializer.Serialize(new { key = stored.Key, bytes = stored.Bytes, sha256 = stored.Sha256, backupClass = request.BackupClass })));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<UploadBackupResponse>.Success(new UploadBackupResponse(stored.Key, stored.Bytes, stored.Sha256));
    }
}
