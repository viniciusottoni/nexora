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

namespace Nexora.Application.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public CreateRoleCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
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

        var codeTaken = await _db.Roles
            .AsNoTracking()
            .AnyAsync(r => r.TenantId == tenantId && r.Code == request.Code && r.DeletedAt == null, cancellationToken);

        if (codeTaken)
        {
            return Result<RoleResponse>.Failure("Já existe um papel com este código.", ApiErrorCodes.RoleCodeAlreadyExists);
        }

        var role = Role.Create(tenantId, request.Code, request.Name, isSystem: false);
        role.UpdatePermissions(JsonSerializer.Serialize(request.Permissions));
        _db.Roles.Add(role);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "ROLE_CREATED",
            entity: "role",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: role.Id,
            before: null,
            after: JsonSerializer.Serialize(new { name = role.Name, permissions = request.Permissions })));

        if (request.Permissions.Count > 0)
        {
            AppendPermissionChange(tenantId, actorId, role.Id, role.Name, added: request.Permissions, removed: Array.Empty<string>(), now);
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Code, role.Name, request.Permissions, role.IsSystem, UserCount: 0));
    }

    private void AppendPermissionChange(
        Guid tenantId,
        Guid actorId,
        Guid roleId,
        string roleName,
        IReadOnlyList<string> added,
        IReadOnlyList<string> removed,
        DateTimeOffset occurredAt)
    {
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "permission.changed",
            aggregateType: "role",
            aggregateId: roleId,
            payload: JsonSerializer.Serialize(new { roleId, added, removed }),
            origin: "CLOUD",
            occurredAt: occurredAt,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

        _db.Alerts.Add(Alert.Create(
            tenantId,
            type: "PERMISSION_CHANGED",
            message: $"Permissões do papel {roleName} foram alteradas",
            targetRoles: new[] { "OWNER", "MANAGER" },
            payload: JsonSerializer.Serialize(new { roleId, actorId, added, removed })));
    }
}
