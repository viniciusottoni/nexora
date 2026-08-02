using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.DeleteModifierGroup;

internal sealed class DeleteModifierGroupCommandHandler : IRequestHandler<DeleteModifierGroupCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<DeleteModifierGroupCommandHandler> _logger;

    public DeleteModifierGroupCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        ILogger<DeleteModifierGroupCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteModifierGroupCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:write"))
        {
            return Result.Failure("Seu perfil não tem permissão para alterar o cardápio.", ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var now = DateTimeOffset.UtcNow;

        var group = await _db.ModifierGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.TenantId == tenantId && g.DeletedAt == null, cancellationToken);

        if (group is null)
        {
            return Result.Failure("Grupo de modificadores não encontrado.", ApiErrorCodes.ModifierGroupNotFound);
        }

        var modifiers = await _db.Modifiers
            .Where(m => m.GroupId == group.Id && m.TenantId == tenantId && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var links = await _db.ProductModifierGroups
            .Where(pg => pg.GroupId == group.Id && pg.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var linkedProductIds = links.Select(l => l.ProductId).ToList();

        // Cascata manual (Domain não tem um "SoftDeleteCascade"): soft delete do grupo e de cada
        // modificador. Os vínculos ficam preservados como referência histórica: a role da aplicação
        // não recebe DELETE físico (ADR-004/Docs Domain 10), e toda leitura funcional parte apenas
        // de grupos com DeletedAt nulo.
        group.SoftDelete();
        foreach (var modifier in modifiers)
        {
            modifier.SoftDelete();
        }

        foreach (var productId in linkedProductIds)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId: tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: productId,
                payload: JsonSerializer.Serialize(new { productId, modifierGroupId = group.Id, removed = true }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                actorId: _tenantContext.UserId,
                deviceId: _tenantContext.DeviceId));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "MODIFIER_GROUP_DELETED",
            entity: "modifier_group",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: group.Id));

        _logger.LogInformation(
            "Grupo de modificadores removido. TenantId={TenantId}, GroupId={GroupId}, ModifiersRemovidos={ModifierCount}, ProdutosAfetados={ProductCount}",
            tenantId, group.Id, modifiers.Count, linkedProductIds.Count);

        return Result.Success();
    }
}
