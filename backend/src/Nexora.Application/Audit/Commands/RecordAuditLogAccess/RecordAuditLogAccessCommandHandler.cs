using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Audit.Commands.RecordAuditLogAccess;

internal sealed class RecordAuditLogAccessCommandHandler : IRequestHandler<RecordAuditLogAccessCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public RecordAuditLogAccessCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public Task<Result> Handle(RecordAuditLogAccessCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Task.FromResult(Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "AUDIT_LOG_ACCESSED",
            entity: "audit_log",
            occurredAt: DateTimeOffset.UtcNow,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            after: JsonSerializer.Serialize(request.Filters)));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Task.FromResult(Result.Success());
    }
}
