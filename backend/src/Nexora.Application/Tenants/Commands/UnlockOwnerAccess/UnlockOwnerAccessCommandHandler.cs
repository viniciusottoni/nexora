using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.UnlockOwnerAccess;

internal sealed class UnlockOwnerAccessCommandHandler : IRequestHandler<UnlockOwnerAccessCommand, Result<UnlockOwnerAccessResponse>>
{
    private readonly IApplicationDbContext _db;

    public UnlockOwnerAccessCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UnlockOwnerAccessResponse>> Handle(UnlockOwnerAccessCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<UnlockOwnerAccessResponse>.Failure("O motivo é obrigatório.", ApiErrorCodes.ReasonRequired);
        }

        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<UnlockOwnerAccessResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var owner = await (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            join user in _db.Users on userRole.UserId equals user.Id
            where userRole.TenantId == tenant.Id && role.Code == "OWNER"
            select user
        ).FirstOrDefaultAsync(cancellationToken);

        if (owner is null)
        {
            return Result<UnlockOwnerAccessResponse>.Failure("Proprietário não encontrado.", ApiErrorCodes.OwnershipOwnerNotFound);
        }

        if (owner.Status != UserStatus.Blocked)
        {
            return Result<UnlockOwnerAccessResponse>.Failure(
                "O proprietário não está bloqueado.", ApiErrorCodes.OwnershipOwnerNotBlocked);
        }

        // Unblock() só reverte Status/FailedAttempts/BlockedUntil — nunca toca PasswordHash (ver
        // AppUser.cs). Nenhuma senha é definida, vista ou logada por este comando (requisito de
        // segurança testável desta US).
        owner.Unblock();

        var now = DateTimeOffset.UtcNow;
        var domainEvent = DomainEvent.Create(
            tenant.Id,
            type: "tenant.owner_access_changed",
            aggregateType: "app_user",
            aggregateId: owner.Id,
            payload: JsonSerializer.Serialize(new
            {
                tenantId = tenant.Id,
                action = "UNLOCKED",
                userId = owner.Id,
                inviteId = (Guid?)null,
                previousOwnerId = (Guid?)null,
                actorId = request.ActorId,
            }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId);
        _db.DomainEvents.Add(domainEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            tenant.Id,
            action: "OWNER_UNLOCKED",
            entity: "app_user",
            occurredAt: now,
            actorId: request.ActorId,
            entityId: owner.Id,
            reason: request.Reason,
            domainEventId: domainEvent.Id));

        return Result<UnlockOwnerAccessResponse>.Success(new UnlockOwnerAccessResponse(owner.Id, "ACTIVE"));
    }
}
