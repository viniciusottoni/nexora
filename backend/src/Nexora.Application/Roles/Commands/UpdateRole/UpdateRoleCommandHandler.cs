using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Roles;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Roles.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateRoleCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<RoleResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.TenantId == tenantId && r.DeletedAt == null, cancellationToken);

        if (role is null)
        {
            return Result<RoleResponse>.Failure("Papel não encontrado.", ApiErrorCodes.RoleNotFound);
        }

        var currentPermissions = JsonSerializer.Deserialize<string[]>(role.Permissions) ?? Array.Empty<string>();
        var nextPermissions = request.Permissions ?? currentPermissions;

        if (role.Code == "OWNER" && !nextPermissions.Contains("*"))
        {
            return Result<RoleResponse>.Failure("O papel OWNER deve manter acesso completo.", ApiErrorCodes.RoleOwnerMustKeepFullAccess);
        }

        var oldName = role.Name;
        var nextName = request.Name ?? role.Name;
        var added = nextPermissions.Except(currentPermissions).ToList();
        var removed = currentPermissions.Except(nextPermissions).ToList();

        role.Rename(nextName);
        role.UpdatePermissions(JsonSerializer.Serialize(nextPermissions));

        var permissionsChanged = added.Count > 0 || removed.Count > 0;

        // EVT-072 permission.changed — criado ANTES do AuditLog (quando aplicável) para
        // correlacionar via DomainEventId (E-09/US-090, "Correlação com o evento").
        DomainEvent? permissionChangedEvent = null;
        if (permissionsChanged)
        {
            permissionChangedEvent = DomainEvent.Create(
                tenantId,
                type: "permission.changed",
                aggregateType: "role",
                aggregateId: role.Id,
                payload: JsonSerializer.Serialize(new { roleId = role.Id, added, removed }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId);
            _db.DomainEvents.Add(permissionChangedEvent);
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: permissionsChanged ? "PERMISSION_CHANGED" : "ROLE_UPDATED",
            entity: "role",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: role.Id,
            before: JsonSerializer.Serialize(new { name = oldName, permissions = currentPermissions }),
            after: JsonSerializer.Serialize(new { name = nextName, permissions = nextPermissions }),
            domainEventId: permissionChangedEvent?.Id));

        if (permissionsChanged)
        {
            _db.Alerts.Add(Alert.Create(
                tenantId,
                type: "PERMISSION_CHANGED",
                message: $"Permissões do papel {nextName} foram alteradas",
                targetRoles: new[] { "OWNER", "MANAGER" },
                payload: JsonSerializer.Serialize(new { roleId = role.Id, actorId, added, removed })));

            var sessionsToRevoke = await _db.AuthSessions
                .Where(s => s.TenantId == tenantId && s.RevokedAt == null &&
                            _db.UserRoles.Any(ur => ur.RoleId == role.Id && ur.UserId == s.UserId))
                .ToListAsync(cancellationToken);

            foreach (var session in sessionsToRevoke)
                session.Revoke();
        }

        var userCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Code, role.Name, nextPermissions, role.IsSystem, userCount));
    }
}
