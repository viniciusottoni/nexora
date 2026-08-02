using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using Microsoft.AspNetCore.Http;

namespace Awaken.Infrastructure.Services;

public class AuditLogService(
    IAuditLogRepository repository,
    IDateTimeService dateTimeService,
    IHttpContextAccessor httpContextAccessor) : IAuditLogService
{
    public async Task RecordAsync(
        string action,
        Guid? actorUserId,
        AuditActorType actorType,
        string resourceType,
        Guid? resourceId,
        string? metadataSafe,
        CancellationToken cancellationToken = default)
    {
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        var entry = AuditLog.Create(
            action,
            actorUserId,
            actorType,
            resourceType,
            resourceId,
            metadataSafe,
            correlationId,
            dateTimeService.UtcNow);

        await repository.AddAsync(entry, cancellationToken);
    }
}
