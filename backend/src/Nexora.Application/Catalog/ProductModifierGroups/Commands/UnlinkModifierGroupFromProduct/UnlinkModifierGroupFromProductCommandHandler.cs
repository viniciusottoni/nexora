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

namespace Nexora.Application.Catalog.ProductModifierGroups.Commands.UnlinkModifierGroupFromProduct;

internal sealed class UnlinkModifierGroupFromProductCommandHandler : IRequestHandler<UnlinkModifierGroupFromProductCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<UnlinkModifierGroupFromProductCommandHandler> _logger;

    public UnlinkModifierGroupFromProductCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        ILogger<UnlinkModifierGroupFromProductCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result> Handle(UnlinkModifierGroupFromProductCommand request, CancellationToken cancellationToken)
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

        var link = await _db.ProductModifierGroups
            .FirstOrDefaultAsync(
                pg => pg.ProductId == request.ProductId && pg.GroupId == request.GroupId && pg.TenantId == tenantId,
                cancellationToken);

        if (link is null)
        {
            return Result.Failure("Este grupo não está vinculado a este produto.", ApiErrorCodes.ProductModifierGroupNotLinked);
        }

        _db.ProductModifierGroups.Remove(link);

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId: tenantId,
            type: "product.updated",
            aggregateType: "product",
            aggregateId: request.ProductId,
            payload: JsonSerializer.Serialize(new { productId = request.ProductId, modifierGroupId = request.GroupId, linked = false }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "PRODUCT_MODIFIER_GROUP_UNLINKED",
            entity: "product_modifier_group",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: request.ProductId));

        _logger.LogInformation(
            "Grupo de modificadores desvinculado de produto. TenantId={TenantId}, ProductId={ProductId}, GroupId={GroupId}",
            tenantId, request.ProductId, request.GroupId);

        return Result.Success();
    }
}
